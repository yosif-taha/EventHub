using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace EventHub.Infrastructure.Auth
{
    public class AuthService(UserManager<ApplicationUser> userManager,
        IJwtProvider jwtProvider,
        IEmailService emailService,
        IHttpContextAccessor httpContext) : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IJwtProvider _jwtProvider = jwtProvider;
        private readonly IEmailService _emailService = emailService;
        private readonly IHttpContextAccessor _httpContext = httpContext;
        private readonly int _refreshTokenExpiryDays = 14;
        public async Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return null!;

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid) return null!;

            // Generate Token
            var (token, expiresIn) = await _jwtProvider.GenerateTokenAsync(user);

            // Generate RefreshToken
            var refreshtoken = GenerateRefreshToken();
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

            // Store RefreshToken in the database
            user.RefreshTokens.Add(new RefreshTokens
            {
                Token = refreshtoken,
                ExpiresOn = refreshTokenExpiration,
            });

            await _userManager.UpdateAsync(user);

            return new AuthResponse(user.Id.ToString(), user.Email, user.FullName, token, expiresIn, refreshtoken, refreshTokenExpiration);

        }
        public async Task<AuthResponse?> GenerateNewTokensAsync(string token, string refreshToken, CancellationToken ct = default)
        {
            var userId = _jwtProvider.ValidateToken(token);
            if (userId is null)
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return null;

            var storedRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken && rt.IsActive);
            if (storedRefreshToken is null)
                return null;

            storedRefreshToken.RevokedOn = DateTime.UtcNow;

            // Generate new Tokens (Jwt token & RefreshToken)

            // Generate Token
            var (newtoken, expiresIn) = await _jwtProvider.GenerateTokenAsync(user);

            // Generate RefreshToken
            var newrefreshtoken = GenerateRefreshToken();
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

            // Store RefreshToken in the database
            user.RefreshTokens.Add(new RefreshTokens
            {
                Token = newrefreshtoken,
                ExpiresOn = refreshTokenExpiration,
            });

            await _userManager.UpdateAsync(user);

            return new AuthResponse(userId, user.Email, user.FullName, newtoken, expiresIn, newrefreshtoken, refreshTokenExpiration);
        }
        public async Task<bool> RegisterAsync(string email, string password, string fullName , UserRole role, CancellationToken ct = default)
        {
            bool emailIsAlreadyUsed = await _userManager.Users.AnyAsync(u => u.Email == email, ct);
            if (emailIsAlreadyUsed)
                return false;

            var user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                FullName = fullName,
                Role = role
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Generate Code
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));  // Encode the code to make it into URL 

                //  Send confirmation email
                await SendConfirmationEmailAsync(user, code);
                return true;
            }

            return false;
        }
        public async Task<bool> ConfirmEmailAsync(string userId, string urlCode)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return false;

            if (user.EmailConfirmed)
                return false;

            var code = urlCode;
            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); // Decode the code from URL
            }
            catch
            {
                return false;
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return result.Succeeded;
        }
        public async Task<bool> ResendConfirmationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return false;

            // Generate Code
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));  // Encode the code to make it into URL 

            // Send confirmation email    
            await SendConfirmationEmailAsync(user, code);
            return true;
        }
        public async Task<bool> SendResetPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return false;
            if(!user.EmailConfirmed)
                 return false; 

            var code = await _userManager.GeneratePasswordResetTokenAsync(user!);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));  // Encode the code to make it into URL 

            // Send confirmation email    
            await SendResetPasswordEmailAsync(user!, code);
            return true;
        }
        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || !user.EmailConfirmed)
                return false;

            var decode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); // Decode the code from URL
            var result = await _userManager.ResetPasswordAsync(user, decode, newPassword);
            return result.Succeeded;
        }
        private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        private async Task SendConfirmationEmailAsync(ApplicationUser user, string code)
        {
            var origin = _httpContext.HttpContext?.Request.Headers.Origin;
            var emailBodey = $"{origin}/auth/emailConfirmation/?userId{user.Id}&code{code}";
            await _emailService.SendEmailAsync(user.Email!, "Confirm your email", emailBodey);
        }
        private async Task SendResetPasswordEmailAsync(ApplicationUser user, string code)
        {
            var origin = _httpContext.HttpContext?.Request.Headers.Origin;
            var emailBodey = $"{origin}/auth/ResetPassword/?email{user.Email}&code{code}";
            await _emailService.SendEmailAsync(user.Email!, "Reset Password", emailBodey);
        }
    }
}
