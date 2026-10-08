using EventHub.Application.Features.Events.GetAll_Events;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class GetAllEventsQueryValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new GetAllEventsQueryValidator();
        var request = CreateValidRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
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
        var validator = new GetAllEventsQueryValidator();
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
        var validator = new GetAllEventsQueryValidator();
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

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Title", true)]
    [InlineData("Location", true)]
    [InlineData("title", false)]
    [InlineData("location", false)]
    [InlineData("   ", false)]
    [InlineData(" Title ", false)]
    [InlineData("Unknown", false)]
    [InlineData("EventDate", false)]
    [InlineData("Status", false)]
    public void Validate_ShouldValidateSortColumn(string? value, bool expectedIsValid)
    {
        // Arrange
        var validator = new GetAllEventsQueryValidator();
        var request = CreateValidRequest() with { SortColumn = value };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.SortColumn), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("asc", true)]
    [InlineData("ASC", true)]
    [InlineData("aSc", true)]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("   ", false)]
    [InlineData(" asc ", false)]
    [InlineData("ascending", false)]
    public void Validate_ShouldValidateSortDirection(string? value, bool expectedIsValid)
    {
        // Arrange
        var validator = new GetAllEventsQueryValidator();
        var request = CreateValidRequest() with { SortDirection = value };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.SortDirection), error.PropertyName));
        }
    }

    private static GetAllEventsQuery CreateValidRequest() =>
        new(null, null, null, "asc", 1, 10);
}
