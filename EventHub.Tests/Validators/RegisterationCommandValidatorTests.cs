using EventHub.Application.Features.Registerations.RegisterationForEvent;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class RegisterationCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new RegisterationCommandValidator();
        var request = CreateValidRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldFail_WhenEventIdIsEmpty()
    {
        // Arrange
        var validator = new RegisterationCommandValidator();
        var request = CreateValidRequest() with { EventId = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.EventId), error.PropertyName);
    }

    private static RegisterationCommand CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
}
