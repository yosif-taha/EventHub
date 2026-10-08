using EventHub.Application.Features.Auth.PasswordReset;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class SendResetPasswordCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new SendResetPasswordCommandValidator();
        var request = CreateValidRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("invalid", false)]
    [InlineData("@example.com", false)]
    [InlineData("user@", false)]
    [InlineData("user@example.com", true)]
    [InlineData("user+tag@example.com", true)]
    public void Validate_ShouldValidateEmailFormat(string? email, bool expectedIsValid)
    {
        // Arrange
        var validator = new SendResetPasswordCommandValidator();
        var request = CreateValidRequest() with { Email = email! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Equal(expectedIsValid, result.IsValid);
        if (expectedIsValid)
        {
            Assert.Empty(result.Errors);
        }
        else
        {
            Assert.NotEmpty(result.Errors);
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Email), error.PropertyName));
        }
    }

    private static SendResetPasswordCommand CreateValidRequest() =>
        new("user@example.com");
}
