using EventHub.Application.Features.Admin.GetAllRegistrations;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class GetAllRegistrationsQueryValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new GetAllRegistrationsQueryValidator();
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
        var validator = new GetAllRegistrationsQueryValidator();
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
        var validator = new GetAllRegistrationsQueryValidator();
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

    private static GetAllRegistrationsQuery CreateValidRequest() =>
        new(1, 10);
}
