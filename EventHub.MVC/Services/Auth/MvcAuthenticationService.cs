using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EventHub.MVC.Services.Auth;

public sealed class MvcAuthenticationService : IMvcAuthenticationService
{
    public ClaimsPrincipal? CreatePrincipal(AuthResponseDto response)
    {
        if (string.IsNullOrWhiteSpace(response.Id) || string.IsNullOrWhiteSpace(response.Token) || response.ExpiresIn <= 0)
            return null;

        JwtSecurityToken token;
        try
        {
            token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, response.Id),
            new(ClaimTypes.Name, response.FullName),
        };

        if (!string.IsNullOrWhiteSpace(response.Email))
            claims.Add(new Claim(ClaimTypes.Email, response.Email));

        foreach (var role in token.Claims
                     .Where(claim => claim.Type is ClaimTypes.Role or "role")
                     .Select(claim => claim.Value)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "MvcCookie", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
