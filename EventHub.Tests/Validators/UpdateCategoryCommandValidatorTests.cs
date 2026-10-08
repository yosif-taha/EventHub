using EventHub.Application.Features.Category.Update_Category;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class UpdateCategoryCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new UpdateCategoryCommandValidator();
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
        var validator = new UpdateCategoryCommandValidator();
        var request = CreateValidRequest() with { Id = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.Id), error.PropertyName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenNameIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new UpdateCategoryCommandValidator();
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
        var validator = new UpdateCategoryCommandValidator();
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

    private static UpdateCategoryCommand CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Technology");
}
