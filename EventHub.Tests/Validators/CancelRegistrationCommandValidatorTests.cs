using EventHub.Application.Features.Registerations.CancelRegistrationForEvent;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class CancelRegistrationCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new CancelRegistrationCommandValidator();
        var request = CreateValidRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldFail_WhenRegistrationIdIsEmpty()
    {
        // Arrange
        var validator = new CancelRegistrationCommandValidator();
        var request = CreateValidRequest() with { RegistrationId = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.RegistrationId), error.PropertyName);
    }

    private static CancelRegistrationCommand CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
}
