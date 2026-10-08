using EventHub.Application.Features.Account.ChangePassword;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class ChangePasswordCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new ChangePasswordCommandValidator();
        var request = CreateValidRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenCurrentPasswordIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new ChangePasswordCommandValidator();
        var request = CreateValidRequest() with { CurrentPassword = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.CurrentPassword), error.PropertyName));
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(9, true)]
    public void Validate_ShouldRespectCurrentPasswordLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new ChangePasswordCommandValidator();
        var request = CreateValidRequest() with { CurrentPassword = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.CurrentPassword), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenNewPasswordIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new ChangePasswordCommandValidator();
        var request = CreateValidRequest() with { NewPassword = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.NewPassword), error.PropertyName));
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(9, true)]
    public void Validate_ShouldRespectNewPasswordLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new ChangePasswordCommandValidator();
        var request = CreateValidRequest() with { NewPassword = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.NewPassword), error.PropertyName));
        }
    }

    private static ChangePasswordCommand CreateValidRequest() =>
        new("abcdefgh", "ijklmnop");
}
