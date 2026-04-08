using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EventHub.Infrastructure.Auth
{
    public class JwtProvider(IOptions<JwtOptions> options, UserManager<ApplicationUser> _userManager) : IJwtProvider
    {
        private readonly JwtOptions _options = options.Value;
        public async Task<(string token, int expiresIn)> GenerateTokenAsync(ApplicationUser user)
        {
            // Create Claims
            var authClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FullName ?? string.Empty),
            };


            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // create signing credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var Credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // generate token
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: authClaims,
                expires: DateTime.UtcNow.AddHours(_options.ExpiresMinutes),
                signingCredentials: Credentials
            );

            return (token: new JwtSecurityTokenHandler().WriteToken(token), _options.ExpiresMinutes * 60);
        }

        public string? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {

                    IssuerSigningKey = key,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                return jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
