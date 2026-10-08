using EventHub.Domin.Enums;
using EventHub.MVC.Models.Organizers;
using EventHub.Tests.Support;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "Unit")]
public class EventEditorValidationTests
{
    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Title_EnforcesLengthBounds(int length, bool valid)
    {
        // Arrange
        var model = Valid(); model.Title = new string('T', length);

        // Act
        var errors = Validate(model);

        // Assert
        Assert.Equal(valid, errors.Count == 0);
        if (!valid) Assert.Contains(errors, e => e.MemberNames.Contains(nameof(model.Title)));
    }

    [Theory]
    [InlineData(EventMode.Online, null, false)]
    [InlineData(EventMode.Online, "https://meet.example.test/room", true)]
    [InlineData(EventMode.Online, "relative", false)]
    [InlineData(EventMode.Offline, null, true)]
    [InlineData(EventMode.Offline, "https://meet.example.test/room", false)]
    public void MeetingUrl_IsConsistentWithMode(EventMode mode, string? url, bool valid)
    {
        // Arrange
        var model = Valid(); model.Mode = mode; model.OnlineMeetingUrl = url;

        // Act
        var errors = Validate(model);

        // Assert
        Assert.Equal(valid, errors.Count == 0);
        if (!valid) Assert.Contains(errors, e => e.MemberNames.Contains(nameof(model.OnlineMeetingUrl)));
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, true)]
    [InlineData(1, -1, false)]
    [InlineData(int.MaxValue, 100, true)]
    public void CapacityAndPrice_EnforceNumericBounds(int capacity, double price, bool valid)
    {
        // Arrange
        var model = Valid(); model.MaxAttendees = capacity; model.Price = price;

        // Act
        var errors = Validate(model);

        // Assert
        Assert.Equal(valid, errors.Count == 0);
    }

    [Fact]
    public void PastDate_IsRejected()
    {
        // Arrange
        var model = Valid(); model.EventDate = new DateTime(2000, 1, 1);

        // Act
        var errors = Validate(model);

        // Assert
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(model.EventDate)));
    }

    internal static EventEditorViewModel Valid() => new() {
        Title = "Conference", Description = "Description", Location = "Cairo", EventDate = WorkflowFixture.Future,
        MaxAttendees = 10, CategoryId = WorkflowFixture.OtherId, Mode = EventMode.Offline
    };
    private static List<ValidationResult> Validate(object model)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), errors, true);
        return errors;
    }
}
