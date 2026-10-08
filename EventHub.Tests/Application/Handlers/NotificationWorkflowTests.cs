using EventHub.Application.Contracts;
using EventHub.Application.Features.Notifications.DispatchPendingNotifications;
using EventHub.Application.Features.Notifications.QueueDueEventReminders;
using EventHub.Application.Features.Notifications.QueueEventNotification;
using EventHub.Application.Features.Notifications.QueueRegistrationConfirmation;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public class NotificationWorkflowTests
{
    [Theory]
    [InlineData(RegistrationStatus.Confirmed, 1)]
    [InlineData(RegistrationStatus.Pending, 0)]
    [InlineData(RegistrationStatus.Canceled, 0)]
    [InlineData(RegistrationStatus.Refunded, 0)]
    public async Task Confirmation_QueuesOnlyConfirmedRegistrationsAndDeduplicates(RegistrationStatus status, int expected)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        var registration = await fixture.SeedRegistrationAsync(entity, status);

        // Act
        var first = await fixture.Mediator.Send(new QueueRegistrationConfirmationCommand(registration.Id));
        var repeated = await fixture.Mediator.Send(new QueueRegistrationConfirmationCommand(registration.Id));

        // Assert
        Assert.True(first.IsSuccess);
        Assert.True(repeated.IsSuccess);
        Assert.Equal(expected, await fixture.Db.Notifications.CountAsync());
        if (expected > 0)
            Assert.Equal("registration-confirmation:" + registration.Id, (await fixture.Db.Notifications.SingleAsync()).DeduplicationKey);
    }

    [Theory]
    [InlineData(12, EventStatus.Scheduled, 1)]
    [InlineData(-12, EventStatus.Scheduled, 0)]
    [InlineData(48, EventStatus.Scheduled, 0)]
    [InlineData(12, EventStatus.Canceled, 0)]
    [InlineData(12, EventStatus.Completed, 0)]
    public async Task DueReminders_UseUpcomingScheduledWindowAndDoNotDuplicate(int hours, EventStatus status, int expected)
    {
        // Arrange: the production clock is not injectable; stay hours away from its boundaries.
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        await fixture.Db.Events.ExecuteUpdateAsync(s => s.SetProperty(e => e.EventDate, DateTime.UtcNow.AddHours(hours)).SetProperty(e => e.Status, status));
        await fixture.SeedRegistrationAsync(entity);
        await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Pending, WorkflowFixture.OtherId);

        // Act
        var first = await fixture.Mediator.Send(new QueueDueEventRemindersCommand());
        var repeated = await fixture.Mediator.Send(new QueueDueEventRemindersCommand());

        // Assert
        Assert.Equal(expected, first.Data);
        Assert.Equal(0, repeated.Data);
        Assert.Equal(expected, await fixture.Db.Notifications.CountAsync());
    }

    [Theory]
    [InlineData(NotificationType.EventReminder, 1)]
    [InlineData(NotificationType.EventCanceled, 2)]
    public async Task EventNotifications_TargetConfirmedAttendeesAndOnlyDeduplicateReminders(NotificationType type, int expected)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        await fixture.SeedRegistrationAsync(entity);
        await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Canceled, WorkflowFixture.OtherId);
        var command = new QueueEventNotificationCommand(entity.Id, type, "Subject", "Message");

        // Act
        await fixture.Mediator.Send(command);
        await fixture.Mediator.Send(command);

        // Assert
        var notifications = await fixture.Db.Notifications.ToListAsync();
        Assert.Equal(expected, notifications.Count);
        Assert.All(notifications, n => { Assert.Equal(WorkflowFixture.AttendeeId, n.UserId); Assert.Equal("Subject", n.Subject); Assert.Equal("Message", n.Message); });
    }

    [Theory]
    [InlineData(NotificationDeliveryStatus.Pending, 0, false, 1)]
    [InlineData(NotificationDeliveryStatus.Failed, 2, false, 1)]
    [InlineData(NotificationDeliveryStatus.Failed, 3, false, 0)]
    [InlineData(NotificationDeliveryStatus.Sent, 1, false, 0)]
    [InlineData(NotificationDeliveryStatus.Processing, 1, true, 1)]
    [InlineData(NotificationDeliveryStatus.Processing, 1, false, 0)]
    public async Task Dispatch_ClaimsOnlyEligibleRowsAndCompletesLease(NotificationDeliveryStatus status, int attempts, bool expired, int sent)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        fixture.Db.Notifications.Add(new Notification {
            EventId = entity.Id, UserId = WorkflowFixture.AttendeeId, Subject = "Subject", Message = "Body",
            NotificationDate = new DateTime(2026, 1, 1), DeliveryStatus = status, DeliveryAttempts = attempts,
            DeliveryLeaseId = status == NotificationDeliveryStatus.Processing ? Guid.Parse("44444444-4444-4444-4444-444444444444") : null,
            DeliveryLeaseExpiresAt = expired ? new DateTime(2000, 1, 1) : WorkflowFixture.Future
        });
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        // Act
        var result = await fixture.Mediator.Send(new DispatchPendingNotificationsCommand());

        // Assert
        Assert.Equal(sent, result.Data);
        var stored = await fixture.Db.Notifications.SingleAsync();
        Assert.Equal(attempts + sent, stored.DeliveryAttempts);
        Assert.Equal(sent == 1 ? NotificationDeliveryStatus.Sent : status, stored.DeliveryStatus);
        fixture.Email.Verify(e => e.SendEmailAsync("attendee@example.test", "Subject", "Body", It.IsAny<CancellationToken>()), Times.Exactly(sent));
        if (sent == 1) { Assert.NotNull(stored.SentAt); Assert.Null(stored.DeliveryLeaseId); Assert.Null(stored.DeliveryLeaseExpiresAt); }
    }

    [Fact]
    public async Task DeliveryFailure_RetriesAtMostThreeTimesAndClearsLease()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        var registration = await fixture.SeedRegistrationAsync(entity);
        await fixture.Mediator.Send(new QueueRegistrationConfirmationCommand(registration.Id));
        fixture.Email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated mail failure"));

        // Act
        for (var attempt = 0; attempt < 4; attempt++)
            Assert.Equal(0, (await fixture.Mediator.Send(new DispatchPendingNotificationsCommand())).Data);

        // Assert
        var stored = await fixture.Db.Notifications.SingleAsync();
        Assert.Equal(3, stored.DeliveryAttempts);
        Assert.Equal(NotificationDeliveryStatus.Failed, stored.DeliveryStatus);
        Assert.Null(stored.SentAt);
        Assert.Null(stored.DeliveryLeaseId);
        fixture.Email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}
