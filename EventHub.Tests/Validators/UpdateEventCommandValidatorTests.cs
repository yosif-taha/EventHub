using EventHub.Application.Features.Events.Update_Event;
using EventHub.Domin.Enums;
using Xunit;

namespace EventHub.Tests.Validators;

public sealed class UpdateEventCommandValidatorTests
{
    [Fact]
    public void Validate_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("     ", true)]
    public void Validate_ShouldAcceptOptionalTitleValues(string? title, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Title = title };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Title), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldPass_WhenDescriptionIsOmittedOrEmpty(string? description)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Description = description };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(199, true)]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validate_ShouldRespectTitleLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Title = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Title), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1999, true)]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void Validate_ShouldRespectDescriptionLengthBoundaries(int length, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Description = new string('a', length) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Description), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(-30, false)]
    [InlineData(30, true)]
    public void Validate_ShouldRejectPastDatesAndAcceptSafelyFutureDates(int daysFromNow, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        // A wide margin avoids relying on the exact construction-time clock boundary.
        var request = CreateValidRequest() with { EventDate = DateTime.UtcNow.AddDays(daysFromNow) };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.EventDate), error.PropertyName));
        }
    }

    [Fact]
    public void Validate_ShouldFail_WhenEventDateIsDefault()
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { EventDate = DateTime.MinValue };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, error => Assert.Equal(nameof(request.EventDate), error.PropertyName));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    public void Validate_ShouldRequirePositiveMaxAttendees(int? maximum, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { MaxAttendees = maximum };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.MaxAttendees), error.PropertyName));
        }
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Id = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(request.Id), error.PropertyName);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(EventMode.Offline, true)]
    [InlineData(EventMode.Online, true)]
    [InlineData((EventMode)(-1), false)]
    [InlineData((EventMode)999, false)]
    public void Validate_ShouldAcceptOnlyDefinedModes(EventMode? mode, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with
        {
            Mode = mode,
            OnlineMeetingUrl = mode == EventMode.Online ? "https://example.com/meeting" : null
        };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.Mode), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("meeting/123", false)]
    [InlineData("not a url", false)]
    [InlineData("https://example.com/meeting", true)]
    [InlineData("http://example.com/meeting", true)]
    [InlineData("mailto:user@example.com", true)]
    public void Validate_ShouldRequireAnAbsoluteUrl_WhenModeIsOnline(string? url, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Mode = EventMode.Online, OnlineMeetingUrl = url };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.OnlineMeetingUrl), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("https://example.com/meeting", false)]
    [InlineData("relative/path", false)]
    public void Validate_ShouldRequireAnEmptyUrl_WhenModeIsOffline(string? url, bool expectedIsValid)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Mode = EventMode.Offline, OnlineMeetingUrl = url };

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
            Assert.All(result.Errors, error => Assert.Equal(nameof(request.OnlineMeetingUrl), error.PropertyName));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("not a url")]
    [InlineData("https://example.com/meeting")]
    public void Validate_ShouldPass_WhenModeIsOmittedRegardlessOfUrl(string? url)
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Mode = null, OnlineMeetingUrl = url };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllOptionalFieldsAreValid()
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with
        {
            Title = "Updated event",
            Description = "Updated description",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Alexandria",
            CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            MaxAttendees = 20,
            Mode = EventMode.Online,
            OnlineMeetingUrl = "https://example.com/meeting"
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldPass_WhenUnvalidatedLocationAndCategoryAreEmpty()
    {
        // Arrange
        var validator = new UpdateEventCommandValidator();
        var request = CreateValidRequest() with { Location = "", CategoryId = Guid.Empty };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private static UpdateEventCommand CreateValidRequest() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), null, null, null, null, null, null, null, null);
}
