using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using Xunit;

namespace EventHub.Tests.Domain;

public sealed class EventTests
{
    private static readonly DateTime ReferenceTime = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(4, 10, 5)]
    [InlineData(9, 10, 10)]
    [InlineData(-1, 10, 0)]
    [InlineData(-3, -1, -2)]
    [InlineData(int.MaxValue - 1, int.MaxValue, int.MaxValue)]
    public void TryIncrementAttendees_WhenBelowCapacity_IncrementsByOneAndReturnsTrue(
        int count, int maximum, int expectedCount)
    {
        // Arrange
        var subject = new Event { CurrentAttendeesCount = count, MaxAttendees = maximum };

        // Act
        var result = subject.TryIncrementAttendees();

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCount, subject.CurrentAttendeesCount);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(11, 10)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void TryIncrementAttendees_WhenAtOrAboveCapacity_ReturnsFalseAndPreservesCount(
        int count, int maximum)
    {
        // Arrange
        var subject = new Event { CurrentAttendeesCount = count, MaxAttendees = maximum };

        // Act
        var result = subject.TryIncrementAttendees();

        // Assert
        Assert.False(result);
        Assert.Equal(count, subject.CurrentAttendeesCount);
    }

    [Fact]
    public void TryIncrementAttendees_IgnoresEventEligibilityAndPreservesAvailabilityProperties()
    {
        // Arrange
        var subject = new Event
        {
            CurrentAttendeesCount = 4,
            MaxAttendees = 10,
            Status = EventStatus.Canceled,
            EventDate = ReferenceTime.AddDays(-1),
            IsCancelled = true,
            IsDeleted = true,
            IsAvailable = false,
            RemainingSlots = 123
        };

        // Act
        var result = subject.TryIncrementAttendees();

        // Assert
        Assert.True(result);
        Assert.Equal(5, subject.CurrentAttendeesCount);
        Assert.Equal(123, subject.RemainingSlots);
        Assert.False(subject.IsAvailable);
    }

    [Theory]
    [InlineData(5, 10, 4)]
    [InlineData(1, 10, 0)]
    [InlineData(12, 10, 11)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue - 1)]
    public void DecrementAttendees_WhenPositive_DecrementsByOne(int count, int maximum, int expectedCount)
    {
        // Arrange
        var subject = new Event { CurrentAttendeesCount = count, MaxAttendees = maximum };

        // Act
        subject.DecrementAttendees();

        // Assert
        Assert.Equal(expectedCount, subject.CurrentAttendeesCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void DecrementAttendees_WhenZeroOrNegative_PreservesCount(int count)
    {
        // Arrange
        var subject = new Event { CurrentAttendeesCount = count, MaxAttendees = 10 };

        // Act
        subject.DecrementAttendees();

        // Assert
        Assert.Equal(count, subject.CurrentAttendeesCount);
    }

    [Fact]
    public void DecrementAttendees_IgnoresCapacityAndEventStateAndPreservesAvailabilityProperties()
    {
        // Arrange
        var subject = new Event
        {
            CurrentAttendeesCount = 5,
            MaxAttendees = 0,
            Status = EventStatus.Canceled,
            IsDeleted = true,
            RemainingSlots = 123,
            IsAvailable = false
        };

        // Act
        subject.DecrementAttendees();

        // Assert
        Assert.Equal(4, subject.CurrentAttendeesCount);
        Assert.Equal(123, subject.RemainingSlots);
        Assert.False(subject.IsAvailable);
    }

    [Theory]
    [InlineData(EventStatus.Scheduled, true)]
    [InlineData(EventStatus.Completed, false)]
    [InlineData(EventStatus.Canceled, false)]
    [InlineData((EventStatus)999, false)]
    public void IsOpenForRegistration_RequiresScheduledStatus(EventStatus status, bool expected)
    {
        // Arrange
        var subject = new Event
        {
            Status = status,
            EventDate = ReferenceTime.AddDays(1),
            CurrentAttendeesCount = 0,
            MaxAttendees = 10
        };

        // Act
        var result = subject.IsOpenForRegistration(ReferenceTime);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(status, subject.Status);
        Assert.Equal(ReferenceTime.AddDays(1), subject.EventDate);
        Assert.Equal(0, subject.CurrentAttendeesCount);
        Assert.Equal(10, subject.MaxAttendees);
    }

    [Theory]
    [InlineData(-1L, false)]
    [InlineData(0L, false)]
    [InlineData(1L, true)]
    public void IsOpenForRegistration_RequiresStrictlyFutureDate(long offsetTicks, bool expected)
    {
        // Arrange
        var subject = new Event
        {
            Status = EventStatus.Scheduled,
            EventDate = ReferenceTime.AddTicks(offsetTicks),
            CurrentAttendeesCount = 0,
            MaxAttendees = 10
        };

        // Act
        var result = subject.IsOpenForRegistration(ReferenceTime);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 10, true)]
    [InlineData(9, 10, true)]
    [InlineData(10, 10, false)]
    [InlineData(11, 10, false)]
    [InlineData(0, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(-1, 10, true)]
    [InlineData(-3, -1, true)]
    public void IsOpenForRegistration_RequiresCountBelowCapacity(int count, int maximum, bool expected)
    {
        // Arrange
        var subject = new Event
        {
            Status = EventStatus.Scheduled,
            EventDate = ReferenceTime.AddDays(1),
            CurrentAttendeesCount = count,
            MaxAttendees = maximum
        };

        // Act
        var result = subject.IsOpenForRegistration(ReferenceTime);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsOpenForRegistration_IgnoresFlagsAndStoredRemainingSlots()
    {
        // Arrange
        var subject = new Event
        {
            Status = EventStatus.Scheduled,
            EventDate = ReferenceTime.AddDays(1),
            CurrentAttendeesCount = 0,
            MaxAttendees = 10,
            IsAvailable = false,
            IsCancelled = true,
            IsDeleted = true,
            RemainingSlots = 0
        };

        // Act
        var result = subject.IsOpenForRegistration(ReferenceTime);

        // Assert
        Assert.True(result);
        Assert.False(subject.IsAvailable);
        Assert.True(subject.IsCancelled);
        Assert.True(subject.IsDeleted);
        Assert.Equal(0, subject.RemainingSlots);
    }

    [Theory]
    [InlineData(EventStatus.Scheduled, EventStatus.Scheduled, false)]
    [InlineData(EventStatus.Scheduled, EventStatus.Completed, true)]
    [InlineData(EventStatus.Scheduled, EventStatus.Canceled, true)]
    [InlineData(EventStatus.Completed, EventStatus.Scheduled, false)]
    [InlineData(EventStatus.Completed, EventStatus.Completed, false)]
    [InlineData(EventStatus.Completed, EventStatus.Canceled, false)]
    [InlineData(EventStatus.Canceled, EventStatus.Scheduled, false)]
    [InlineData(EventStatus.Canceled, EventStatus.Completed, false)]
    [InlineData(EventStatus.Canceled, EventStatus.Canceled, false)]
    [InlineData(EventStatus.Scheduled, (EventStatus)999, false)]
    [InlineData((EventStatus)999, EventStatus.Scheduled, false)]
    [InlineData((EventStatus)999, EventStatus.Completed, false)]
    [InlineData((EventStatus)999, EventStatus.Canceled, false)]
    [InlineData((EventStatus)999, (EventStatus)999, false)]
    public void CanTransitionTo_ReturnsExpectedResultWithoutChangingStatus(
        EventStatus currentStatus, EventStatus targetStatus, bool expected)
    {
        // Arrange
        var subject = new Event { Status = currentStatus };

        // Act
        var result = subject.CanTransitionTo(targetStatus);

        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(currentStatus, subject.Status);
    }

    [Theory]
    [InlineData(EventStatus.Completed)]
    [InlineData(EventStatus.Canceled)]
    public void CanTransitionTo_IgnoresDateCapacityAndFlags(EventStatus targetStatus)
    {
        // Arrange
        var subject = new Event
        {
            Status = EventStatus.Scheduled,
            EventDate = ReferenceTime.AddDays(1),
            CurrentAttendeesCount = 10,
            MaxAttendees = 10,
            IsCancelled = true,
            IsDeleted = true
        };

        // Act
        var result = subject.CanTransitionTo(targetStatus);

        // Assert
        Assert.True(result);
        Assert.Equal(EventStatus.Scheduled, subject.Status);
    }
}
