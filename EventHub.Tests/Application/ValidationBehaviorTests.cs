using EventHub.Application.Common.Behaviors;
using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Category.Create_Category;
using FluentValidation;
using Xunit;

namespace EventHub.Tests.Application;

public sealed class ValidationBehaviorTests
{
    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "Valid")]
    public async Task Handle_InvokesNext_WhenValidationIsAbsentOrSuccessful(bool hasValidator, string name)
    {
        // Arrange
        var validators = hasValidator ? new IValidator<CreateCategoryCommand>[] { new CreateCategoryCommandValidator() } : [];
        var behavior = new ValidationBehavior<CreateCategoryCommand, RequestResult<Guid>>(validators);
        var expected = RequestResult<Guid>.Success(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var calls = 0;

        // Act
        var result = await behavior.Handle(new(name), _ => { calls++; return Task.FromResult(expected); }, default);

        // Assert
        Assert.Same(expected, result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Handle_ReturnsValidationFailureWithoutInvokingNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateCategoryCommand, RequestResult<Guid>>([new CreateCategoryCommandValidator()]);
        var calls = 0;

        // Act
        var result = await behavior.Handle(new(""), _ => { calls++; return Task.FromResult(RequestResult<Guid>.Success(Guid.Empty)); }, default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.Equal(0, calls);
    }
}
