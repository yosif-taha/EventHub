using EventHub.Application.Features.Auth.Register;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class RegisterCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
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
        var validator = new RegisterCommandValidator();
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
        var validator = new RegisterCommandValidator();
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
        var validator = new RegisterCommandValidator();
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenFullNameIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var request = CreateValidRequest() with { FullName = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.FullName), error.PropertyName));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(99, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validate_ShouldRespectFullNameLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var request = CreateValidRequest() with { FullName = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.FullName), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldPass_WhenPhoneNumberIsNullEmptyOrWhitespace(string? value)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var request = CreateValidRequest() with { PhoneNumber = value };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(19, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void Validate_ShouldRespectPhoneNumberLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var request = CreateValidRequest() with { PhoneNumber = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.PhoneNumber), error.PropertyName));
        }
    }

    private static RegisterCommand CreateValidRequest() =>
        new("user@example.com", "abcdefgh", "Test User", null);
}
