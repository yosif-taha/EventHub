using EventHub.Infrastructure.Payment;
using EventHub.Persistence.DataSeeding;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EventHub.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class SettingsValidationTests
{
    [Theory]
    [InlineData(false, null, null, null, true)]
    [InlineData(true, "admin@example.test", "12345678", "Admin", true)]
    [InlineData(true, "invalid", "12345678", "Admin", false)]
    [InlineData(true, "admin@example.test", "1234567", "Admin", false)]
    [InlineData(true, "admin@example.test", "12345678", " ", false)]
    public void BootstrapSettings_RequireValidCredentialsOnlyWhenEnabled(bool enabled, string? email, string? password, string? name, bool valid)
    {
        // Arrange
        var settings = new BootstrapAdminSettings { Enabled = enabled, Email = email, Password = password, FullName = name };

        // Act
        var result = Validator.TryValidateObject(settings, new ValidationContext(settings), [], true);

        // Assert
        Assert.Equal(valid, result);
    }

    [Theory]
    [InlineData("https://provider.example.test/", "https://app.example.test/{registrationId}", true)]
    [InlineData("http://provider.example.test/", "http://app.example.test/return", true)]
    [InlineData("relative", "https://app.example.test/return", false)]
    [InlineData("https://provider.example.test/", "relative", false)]
    [InlineData("ftp://provider.example.test/", "https://app.example.test/return", false)]
    public void PaymentSettings_ValidateConfiguredUrlSchemes(string baseUrl, string returnUrl, bool valid)
    {
        // Arrange
        var settings = new PaymobSettings { ApiKey = "key", HmacSecret = "secret", CardIntegrationId = "1", IframeId = "2", BaseUrl = baseUrl, ReturnUrl = returnUrl };

        // Act
        var result = Validator.TryValidateObject(settings, new ValidationContext(settings), [], true);

        // Assert
        Assert.Equal(valid, result);
    }
}
