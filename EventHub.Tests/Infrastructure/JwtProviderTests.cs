using EventHub.Domin.Models;
using EventHub.Infrastructure.Auth;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace EventHub.Tests.Infrastructure;

[Trait("Category", "MockUnit")]
public class JwtProviderTests
{
    [Fact]
    public async Task GeneratedToken_ContainsIdentityRolesAndConfiguredLifetime()
    {
        // Arrange
        var manager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var user = new ApplicationUser { Id = WorkflowFixture.AttendeeId, Email = "user@example.test", FullName = "Test User" };
        manager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new[] { "Admin", "Organizer" });
        var provider = new JwtProvider(Options.Create(Settings()), manager.Object);

        // Act
        var generated = await provider.GenerateTokenAsync(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generated.token);

        // Assert
        Assert.Equal(1800, generated.expiresIn);
        Assert.Equal("tests", token.Issuer);
        Assert.Contains("tests", token.Audiences);
        Assert.Contains(token.Claims, c => c.Value == "Admin");
        Assert.Contains(token.Claims, c => c.Value == "Organizer");
        Assert.Contains(token.Claims, c => c.Value == "Test User");
        Assert.Equal(user.Id.ToString(), provider.ValidateToken(generated.token));
    }

    [Theory]
    [InlineData("valid", true)]
    [InlineData("issuer", false)]
    [InlineData("audience", false)]
    [InlineData("signature", false)]
    [InlineData("expired", false)]
    [InlineData("malformed", false)]
    public void Validate_EnforcesIssuerAudienceSignatureAndExpiration(string scenario, bool valid)
    {
        // Arrange
        var settings = Settings();
        var token = new JwtSecurityToken(
            scenario == "issuer" ? "wrong" : "tests", scenario == "audience" ? "wrong" : "tests",
            [new Claim(JwtRegisteredClaimNames.Sub, WorkflowFixture.AttendeeId.ToString())],
            expires: scenario == "expired" ? new DateTime(2000, 1, 1) : WorkflowFixture.Future,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                scenario == "signature" ? "another-test-only-signing-key-with-32-bytes" : settings.Key)), SecurityAlgorithms.HmacSha256));
        var encoded = scenario == "malformed" ? "invalid" : new JwtSecurityTokenHandler().WriteToken(token);
        var provider = new JwtProvider(Options.Create(settings), null!);

        // Act
        var result = provider.ValidateToken(encoded);

        // Assert
        Assert.Equal(valid ? WorkflowFixture.AttendeeId.ToString() : null, result);
    }

    private static JwtOptions Settings() => new() { Key = ApiFixture.SigningKey, Issuer = "tests", Audience = "tests", ExpiresMinutes = 30 };
}
