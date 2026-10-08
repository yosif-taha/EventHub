using EventHub.MVC.Services.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "Unit")]
public class MvcAuthenticationTests
{
    [Theory]
    [InlineData("", "token", 60)]
    [InlineData("user", "", 60)]
    [InlineData("user", "not-a-jwt", 60)]
    [InlineData("user", "token", 0)]
    [InlineData("user", "token", -1)]
    public void InvalidResponse_CannotCreateCookiePrincipal(string id, string token, int expiry)
    {
        // Arrange
        var service = new MvcAuthenticationService();

        // Act
        var principal = service.CreatePrincipal(new AuthResponseDto { Id = id, Token = token, ExpiresIn = expiry });

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidResponse_MapsIdentityAndDeduplicatesRoles()
    {
        // Arrange
        var token = new JwtSecurityToken(claims: [
            new Claim("role", "Admin"), new Claim("role", "admin"), new Claim(ClaimTypes.Role, "Organizer")]);
        var response = new AuthResponseDto {
            Id = "user-id", Email = "user@example.test", FullName = "Test User", ExpiresIn = 60,
            Token = new JwtSecurityTokenHandler().WriteToken(token)
        };

        // Act
        var principal = new MvcAuthenticationService().CreatePrincipal(response);

        // Assert
        Assert.NotNull(principal);
        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.Equal("Test User", principal.Identity.Name);
        Assert.Equal("user-id", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("user@example.test", principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(2, principal.FindAll(ClaimTypes.Role).Count());
        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.IsInRole("Organizer"));
    }
}
