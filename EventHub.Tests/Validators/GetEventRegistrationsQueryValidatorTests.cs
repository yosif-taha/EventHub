using EventHub.Application.Features.Registerations.GetEventRegistrations;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class GetEventRegistrationsQueryValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new GetEventRegistrationsQueryValidator();
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
        var validator = new GetEventRegistrationsQueryValidator();
        var request = CreateValidRequest() with { EventId = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.EventId), error.PropertyName);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(int.MaxValue, true)]
    public void Validate_ShouldRespectPageNumberBoundaries(int value, bool expectedIsValid)
    {
        // Arrange
        var validator = new GetEventRegistrationsQueryValidator();
        var request = CreateValidRequest() with { PageNumber = value };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.PageNumber), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(49, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void Validate_ShouldRespectPageSizeBoundaries(int value, bool expectedIsValid)
    {
        // Arrange
        var validator = new GetEventRegistrationsQueryValidator();
        var request = CreateValidRequest() with { PageSize = value };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.PageSize), error.PropertyName));
        }
    }

    private static GetEventRegistrationsQuery CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), 1, 10);
}
