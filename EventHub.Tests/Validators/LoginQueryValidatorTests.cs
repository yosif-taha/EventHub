using EventHub.Application.Features.Auth.Login;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class LoginQueryValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new LoginQueryValidator();
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
        var validator = new LoginQueryValidator();
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenPasswordIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new LoginQueryValidator();
        var request = CreateValidRequest() with { Password = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.Password), error.PropertyName));
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(9, true)]
    public void Validate_ShouldRespectPasswordLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new LoginQueryValidator();
        var request = CreateValidRequest() with { Password = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Password), error.PropertyName));
        }
    }

    private static LoginQuery CreateValidRequest() =>
        new("user@example.com", "abcdefgh");
}
