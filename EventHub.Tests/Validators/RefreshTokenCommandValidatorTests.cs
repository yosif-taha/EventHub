using EventHub.Application.Features.Auth.RefreshTokens;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class RefreshTokenCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
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
    public void Validate_ShouldFail_WhenTokenIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
        var request = CreateValidRequest() with { Token = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.Token), error.PropertyName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenRefreshTokenIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
        var request = CreateValidRequest() with { RefreshToken = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.RefreshToken), error.PropertyName));
    }

    [Theory]
    [InlineData(19, false)]
    [InlineData(20, true)]
    [InlineData(21, true)]
    public void Validate_ShouldRespectRefreshTokenLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new RefreshTokenCommandValidator();
        var request = CreateValidRequest() with { RefreshToken = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.RefreshToken), error.PropertyName));
        }
    }

    private static RefreshTokenCommand CreateValidRequest() =>
        new("not-a-jwt", "abcdefghijklmnopqrst");
}
