using EventHub.Application.Features.Auth.ConfirmEmail;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class ConfirmEmailCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new ConfirmEmailCommandValidator();
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
    public void Validate_ShouldFail_WhenUserIdIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new ConfirmEmailCommandValidator();
        var request = CreateValidRequest() with { UserId = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.UserId), error.PropertyName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenCodeIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new ConfirmEmailCommandValidator();
        var request = CreateValidRequest() with { Code = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.Code), error.PropertyName));
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(11, true)]
    public void Validate_ShouldRespectCodeLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new ConfirmEmailCommandValidator();
        var request = CreateValidRequest() with { Code = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Code), error.PropertyName));
        }
    }

    private static ConfirmEmailCommand CreateValidRequest() =>
        new("user-id", "abcdefghij");
}
