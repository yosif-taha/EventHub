using EventHub.Application.Features.Events.Update_Event_Status;
using EventHub.Domin.Enums;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class UpdateEventStatusCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new UpdateEventStatusCommandValidator();
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
        var validator = new UpdateEventStatusCommandValidator();
        var request = CreateValidRequest() with { Id = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.Id), error.PropertyName);
    }

    [Theory]
    [InlineData(EventStatus.Completed, true)]
    [InlineData(EventStatus.Canceled, true)]
    [InlineData(EventStatus.Scheduled, false)]
    [InlineData((EventStatus)(-1), false)]
    [InlineData((EventStatus)999, false)]
    public void Validate_ShouldAcceptOnlySupportedStatusValues(EventStatus value, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventStatusCommandValidator();
        var request = CreateValidRequest() with { Status = value };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Status), error.PropertyName));
        }
    }

    private static UpdateEventStatusCommand CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), EventStatus.Completed);
}
