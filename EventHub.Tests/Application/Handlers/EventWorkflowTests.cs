using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Events.Create_Event;
using EventHub.Application.Features.Events.Update_Event;
using EventHub.Application.Features.Events.Update_Event_Status;
using EventHub.Application.Features.Events.Delete_Event;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public class EventWorkflowTests
{
    [Theory]
    [InlineData(RoleNames.Admin, true)]
    [InlineData(RoleNames.Organizer, true)]
    [InlineData(RoleNames.Attendee, false)]
    public async Task Create_RequiresManagementRoleAndMapsFields(string role, bool allowed)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var existing = await fixture.SeedEventAsync();
        fixture.User.Role = role;
        fixture.User.UserId = WorkflowFixture.OwnerId;
        var command = new CreateEventCommand("Online conference", "New description", WorkflowFixture.Future, 25, "Online", existing.CategoryId!.Value, 20, EventMode.Online, "https://meet.example.test/room");

        // Act
        var result = await fixture.Mediator.Send(command);

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? 2 : 1, await fixture.Db.Events.CountAsync());
        if (!allowed) { Assert.Equal(ErrorCode.Forbidden, result.ErrorCode); return; }
        var created = await fixture.Db.Events.SingleAsync(e => e.Id == result.Data);
        Assert.Equal(command.Title, created.Title);
        Assert.Equal(command.Description, created.Description);
        Assert.Equal(command.EventDate, created.EventDate);
        Assert.Equal(25m, created.Price);
        Assert.Equal(20, created.MaxAttendees);
        Assert.Equal(WorkflowFixture.OwnerId, created.OrganizerId);
        Assert.Equal(EventStatus.Scheduled, created.Status);
        Assert.Equal(command.OnlineMeetingUrl, created.OnlineMeetingUrl);
    }

    [Theory]
    [InlineData(RoleNames.Organizer, true, EventStatus.Scheduled, true)]
    [InlineData(RoleNames.Admin, false, EventStatus.Scheduled, true)]
    [InlineData(RoleNames.Organizer, false, EventStatus.Scheduled, false)]
    [InlineData(RoleNames.Attendee, true, EventStatus.Scheduled, false)]
    [InlineData(RoleNames.Organizer, true, EventStatus.Completed, false)]
    [InlineData(RoleNames.Organizer, true, EventStatus.Canceled, false)]
    public async Task Update_EnforcesOwnershipAndScheduledState(string role, bool owner, EventStatus status, bool allowed)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        await fixture.SeedRegistrationAsync(entity);
        await fixture.Db.Events.ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, status));
        fixture.User.Role = role;
        fixture.User.UserId = owner ? WorkflowFixture.OwnerId : WorkflowFixture.OtherId;

        // Act
        var result = await fixture.Mediator.Send(Update(entity.Id) with { Title = "Updated title" });

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? "Updated title" : entity.Title, (await fixture.Db.Events.SingleAsync()).Title);
        Assert.Equal(allowed ? 1 : 0, await fixture.Db.Notifications.CountAsync());
        if (!allowed)
            Assert.Equal(role == RoleNames.Attendee || !owner ? ErrorCode.Forbidden : ErrorCode.EventInvalidStatusTransition, result.ErrorCode);
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    public async Task Update_CapacityCannotDropBelowExistingAttendees(int capacity, bool allowed)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(count: 5);
        fixture.User.Role = RoleNames.Admin;

        // Act
        var result = await fixture.Mediator.Send(Update(entity.Id) with { MaxAttendees = capacity });

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? capacity : 10, (await fixture.Db.Events.SingleAsync()).MaxAttendees);
        if (!allowed) Assert.Equal(ErrorCode.EventCapacityFull, result.ErrorCode);
    }

    [Fact]
    public async Task Update_ModeSwitchClearsMeetingUrlAndUnchangedDetailsDoNotNotify()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        await fixture.SeedRegistrationAsync(entity);
        fixture.User.Role = RoleNames.Admin;

        // Act
        var unchanged = await fixture.Mediator.Send(Update(entity.Id) with { Title = entity.Title });
        var online = await fixture.Mediator.Send(Update(entity.Id) with { Mode = EventMode.Online, OnlineMeetingUrl = "https://meet.example.test/room" });
        var offline = await fixture.Mediator.Send(Update(entity.Id) with { Mode = EventMode.Offline });

        // Assert
        Assert.True(unchanged.IsSuccess);
        Assert.True(online.IsSuccess);
        Assert.True(offline.IsSuccess);
        Assert.Equal(2, await fixture.Db.Notifications.CountAsync());
        var stored = await fixture.Db.Events.SingleAsync();
        Assert.Equal(EventMode.Offline, stored.Mode);
        Assert.Null(stored.OnlineMeetingUrl);
    }

    [Theory]
    [InlineData(EventStatus.Scheduled, EventStatus.Canceled, true, 1)]
    [InlineData(EventStatus.Scheduled, EventStatus.Completed, true, 0)]
    [InlineData(EventStatus.Completed, EventStatus.Canceled, false, 0)]
    [InlineData(EventStatus.Canceled, EventStatus.Scheduled, false, 0)]
    public async Task StatusChange_EnforcesDomainTransitionsAndNotifiesCancellation(EventStatus current, EventStatus next, bool allowed, int notifications)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        await fixture.SeedRegistrationAsync(entity);
        await fixture.Db.Events.ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, current));
        fixture.User.Role = RoleNames.Admin;

        // Act
        var result = await fixture.Mediator.Send(new UpdateEventStatusCommand(entity.Id, next));

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? next : current, (await fixture.Db.Events.SingleAsync()).Status);
        Assert.Equal(notifications, await fixture.Db.Notifications.CountAsync());
    }

    [Theory]
    [InlineData(RoleNames.Admin, true)]
    [InlineData(RoleNames.Organizer, false)]
    [InlineData(RoleNames.Attendee, false)]
    public async Task Delete_IsAdminOnlyAndSoftDeletes(string role, bool allowed)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        fixture.User.Role = role;

        // Act
        var result = await fixture.Mediator.Send(new DeleteEventCommand(entity.Id));

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed, (await fixture.Db.Events.IgnoreQueryFilters().SingleAsync()).IsDeleted);
        Assert.Equal(allowed ? 0 : 1, await fixture.Db.Events.CountAsync());
    }

    private static UpdateEventCommand Update(Guid id) => new(id, null, null, null, null, null, null, null, null);
}
