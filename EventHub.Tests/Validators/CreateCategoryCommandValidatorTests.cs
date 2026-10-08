using EventHub.Application.Features.Category.Create_Category;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class CreateCategoryCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new CreateCategoryCommandValidator();
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
    public void Validate_ShouldFail_WhenNameIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new CreateCategoryCommandValidator();
        var request = CreateValidRequest() with { Name = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.Name), error.PropertyName));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(99, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validate_ShouldRespectNameLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new CreateCategoryCommandValidator();
        var request = CreateValidRequest() with { Name = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Name), error.PropertyName));
        }
    }

    private static CreateCategoryCommand CreateValidRequest() =>
        new("Technology");
}
