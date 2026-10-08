using EventHub.Application.Features.Events.Check_Event_Availability;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class CheckEventAvailabilityQueryValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new CheckEventAvailabilityQueryValidator();
        var request = CreateValidRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var validator = new CheckEventAvailabilityQueryValidator();
        var request = CreateValidRequest() with { Id = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.Id), error.PropertyName);
    }

    private static CheckEventAvailabilityQuery CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
}
