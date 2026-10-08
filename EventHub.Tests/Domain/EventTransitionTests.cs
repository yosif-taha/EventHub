using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using Xunit;

namespace EventHub.Tests.Domain;

public sealed class EventTransitionTests
{
    [Theory]
    [InlineData(EventStatus.Scheduled, EventStatus.Completed)]
    [InlineData(EventStatus.Completed, EventStatus.Scheduled)]
    [InlineData(EventStatus.Canceled, (EventStatus)999)]
    public void TransitionTo_AssignsRequestedStatusWithoutEnforcingEligibility(EventStatus initial, EventStatus target)
    {
        // Arrange
        var subject = new Event { Status = initial, CurrentAttendeesCount = 3 };

        // Act
        subject.TransitionTo(target);

        // Assert
        Assert.Equal(target, subject.Status);
        Assert.Equal(3, subject.CurrentAttendeesCount);
    }
}
