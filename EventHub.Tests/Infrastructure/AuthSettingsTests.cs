using EventHub.Infrastructure.Auth;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace EventHub.Tests.Infrastructure;

[Trait("Category", "MockUnit")]
public class AuthSettingsTests
{
    [Theory]
    [InlineData("Development", "http://localhost:5000/", true)]
    [InlineData("Production", "http://app.example.test/", false)]
    [InlineData("Production", "https://app.example.test/", true)]
    [InlineData("Production", "https://app.example.test/path/", true)]
    [InlineData("Production", "https://app.example.test/path", false)]
    [InlineData("Production", "https://user:pass@app.example.test/", false)]
    [InlineData("Production", "https://app.example.test/?a=1", false)]
    [InlineData("Production", "https://app.example.test/#fragment", false)]
    [InlineData("Development", "ftp://app.example.test/", false)]
    [InlineData("Development", "/relative/", false)]
    [InlineData("Development", "", false)]
    public void PublicBaseUrl_EnforcesEnvironmentAndSafeLinkRequirements(string environment, string url, bool valid)
    {
        // Arrange
        var host = new Mock<IHostEnvironment>();
        host.SetupGet(h => h.EnvironmentName).Returns(environment);
        var validator = new AuthSettingsValidator(host.Object);

        // Act
        var result = validator.Validate(null, new AuthSettings { PublicBaseUrl = url });

        // Assert
        Assert.Equal(valid, result.Succeeded);
        Assert.Equal(!valid, result.Failed);
    }
}
