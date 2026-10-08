using EventHub.Application.Features.Admin.GetUsers;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class GetUsersQueryValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new GetUsersQueryValidator();
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
        var validator = new GetUsersQueryValidator();
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
    [InlineData(99, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validate_ShouldRespectPageSizeBoundaries(int value, bool expectedIsValid)
    {
        // Arrange
        var validator = new GetUsersQueryValidator();
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
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldPass_WhenSearchValueIsNullEmptyOrWhitespace(string? value)
    {
        // Arrange
        var validator = new GetUsersQueryValidator();
        var request = CreateValidRequest() with { SearchValue = value };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(199, true)]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validate_ShouldRespectSearchValueLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new GetUsersQueryValidator();
        var request = CreateValidRequest() with { SearchValue = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.SearchValue), error.PropertyName));
        }
    }

    private static GetUsersQuery CreateValidRequest() =>
        new(1, 10, null);
}
