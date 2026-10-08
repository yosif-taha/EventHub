using EventHub.Application.Features.Notifications.SendEventAnnouncement;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class SendEventAnnouncementCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new SendEventAnnouncementCommandValidator();
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
        var validator = new SendEventAnnouncementCommandValidator();
        var request = CreateValidRequest() with { EventId = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.EventId), error.PropertyName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenSubjectIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new SendEventAnnouncementCommandValidator();
        var request = CreateValidRequest() with { Subject = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.Subject), error.PropertyName));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(249, true)]
    [InlineData(250, true)]
    [InlineData(251, false)]
    public void Validate_ShouldRespectSubjectLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new SendEventAnnouncementCommandValidator();
        var request = CreateValidRequest() with { Subject = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Subject), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenMessageIsMissingOrWhitespace(string? value)
    {
        // Arrange
        var validator = new SendEventAnnouncementCommandValidator();
        var request = CreateValidRequest() with { Message = value! };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.Message), error.PropertyName));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(999, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Validate_ShouldRespectMessageLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new SendEventAnnouncementCommandValidator();
        var request = CreateValidRequest() with { Message = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Message), error.PropertyName));
        }
    }

    private static SendEventAnnouncementCommand CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Event news", "The venue has changed.");
}
