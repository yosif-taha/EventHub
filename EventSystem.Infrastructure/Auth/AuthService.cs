using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EventHub.Infrastructure.Auth
{
    public class AuthService(UserManager<ApplicationUser> _userManager,
        IJwtProvider _jwtProvider,
        IEmailService _emailService,
        IHttpContextAccessor _httpContext) : IAuthService
    {

        private readonly int _refreshTokenExpiryDays = 14;
        public async Task<RequestResult<AuthResponse?>> LoginAsync(string email, string password, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return RequestResult<AuthResponse?>.Failure(ErrorCode.UserNotFound);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
                return RequestResult<AuthResponse?>.Failure(ErrorCode.InvalidCredentials);
            try
            {
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
                return RequestResult<AuthResponse?>.Success(new AuthResponse(user.Id.ToString(), user.Email, user.FullName, token, expiresIn, refreshtoken, refreshTokenExpiration));

            }
            catch
            {
                return RequestResult<AuthResponse?>.Failure(ErrorCode.InternalServerError);
            }
        }
        public async Task<RequestResult<Guid>> RegisterAsync(string email, string password, string fullName, UserRole role, CancellationToken ct)
        {
            bool emailIsAlreadyUsed = await _userManager.Users.AnyAsync(u => u.Email == email, ct);
            if (emailIsAlreadyUsed)
                return RequestResult<Guid>.Failure(ErrorCode.UserAlreadyExist);

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
                return RequestResult<Guid>.Success(user.Id);
            }

            return RequestResult<Guid>.Failure(ErrorCode.DatabaseError);
        }
        public async Task<RequestResult<AuthResponse?>> GenerateNewTokensAsync(string token, string refreshToken, CancellationToken ct)
        {
           
            var userId = _jwtProvider.ValidateToken(token);
            if (userId is null)
                return RequestResult<AuthResponse?>.Failure(ErrorCode.InvalidCredentials);

           
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return RequestResult<AuthResponse?>.Failure(ErrorCode.UserNotFound);

             
            var storedRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken && rt.IsActive);
            if (storedRefreshToken is null)
                return RequestResult<AuthResponse?>.Failure(ErrorCode.InvalidRefreshToken);

            storedRefreshToken.RevokedOn = DateTime.UtcNow;

            // Generate new Tokens (Jwt token & RefreshToken)
            try
            {
               
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
                return RequestResult<AuthResponse?>.Success(new AuthResponse(userId, user.Email, user.FullName, newtoken, expiresIn, newrefreshtoken, refreshTokenExpiration));
            }
            catch
            { 
                return RequestResult<AuthResponse?>.Failure(ErrorCode.InternalServerError);
            }
        }
        public async Task<RequestResult<bool>> ConfirmEmailAsync(string userId, string urlCode , CancellationToken ct)
        {
           
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return RequestResult<bool>.Failure(ErrorCode.UserNotFound);

            if (user.EmailConfirmed)
                return RequestResult<bool>.Failure(ErrorCode.EmailAlreadyConfirmed);

           
            var code = urlCode;
            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); // Decode the code from URL
            }
            catch
            {
               return RequestResult<bool>.Failure(ErrorCode.InvalidCredentials);
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
           
            if (!result.Succeeded)
                return RequestResult<bool>.Failure(ErrorCode.EmailNotConfirmed);
            return RequestResult<bool>.Success(true);
        }
        public async Task<RequestResult<bool>> ResendConfirmationEmailAsync(string email , CancellationToken ct)
        {
           
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return RequestResult<bool>.Failure(ErrorCode.UserNotFound);
           
            // Generate Code
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));  // Encode the code to make it into URL 
            if (code is null)
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError);
           
            // Send confirmation email    
            await SendConfirmationEmailAsync(user, code);    
           
            return RequestResult<bool>.Success(true);
        }
        public async Task<RequestResult<bool>> SendResetPasswordAsync(string email , CancellationToken ct)
        {
           
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
               return RequestResult<bool>.Failure(ErrorCode.UserNotFound);  
            if (!user.EmailConfirmed)
                return RequestResult<bool>.Failure(ErrorCode.EmailAlreadyConfirmed);
           

            var code = await _userManager.GeneratePasswordResetTokenAsync(user!);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));  // Encode the code to make it into URL 
            if (code is null)
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError);  
           

            // Send confirmation email    
            await SendResetPasswordEmailAsync(user!, code);
           
            return RequestResult<bool>.Success(true);
        }
        public async Task<RequestResult<bool>> ResetPasswordAsync(string email, string code, string newPassword , CancellationToken ct)
        {
           
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
               return RequestResult<bool>.Failure(ErrorCode.UserNotFound);
            if (!user.EmailConfirmed)
                return RequestResult<bool>.Failure(ErrorCode.EmailNotConfirmed);

            var decode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); // Decode the code from URL
           
            var result = await _userManager.ResetPasswordAsync(user, decode, newPassword);
            if (!result.Succeeded)
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError);
           
            return RequestResult<bool>.Success(true);
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
