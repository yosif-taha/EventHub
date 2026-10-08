using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Events.GetAll_Events;
using EventHub.Application.Features.Events.Get_Event_By_Id;
using EventHub.Application.Features.Events.Check_Event_Availability;
using EventHub.Application.Features.Events.GetManagedEventById;
using EventHub.Application.Features.Events.GetMyEvents;
using EventHub.Application.Features.Registerations.GetMyEventMeetingLink;
using EventHub.Application.Features.Registerations.GetMyRegistrationStatus;
using EventHub.Application.Features.Registerations.GetUserRegistrations;
using EventHub.Application.Features.Registerations.GetEventRegistrations;
using EventHub.Application.Features.Admin.GetAllRegistrations;
using EventHub.Application.Features.Dashboards.GetAdminDashboard;
using EventHub.Application.Features.Dashboards.GetOrganizerDashboard;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public class QueryWorkflowTests
{
    [Fact]
    public async Task Events_FilterSortPaginateAndProjectBusinessFields()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var first = await fixture.SeedEventAsync(price: 25, count: 3);
        var second = await fixture.SeedEventAsync();
        await fixture.Db.Events.Where(e => e.Id == second.Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.Title, "Zebra seminar"));

        // Act
        var sorted = await fixture.Mediator.Send(new GetAllEventsQuery(null, null, "Title", "desc", 1, 1));
        var filtered = await fixture.Mediator.Send(new GetAllEventsQuery("Community", first.CategoryId, "Title"));
        var detail = await fixture.Mediator.Send(new GetEventByIdQuery(first.Id));

        // Assert
        Assert.Equal(2, sorted.Data!.TotalCount);
        Assert.Equal(second.Id, Assert.Single(sorted.Data.Items).Id);
        Assert.True(sorted.Data.HasNextPage);
        Assert.Equal(first.Id, Assert.Single(filtered.Data!.Items).Id);
        Assert.Equal(7, detail.Data!.RemainingSlots);
        Assert.Equal(first.Category.Name, detail.Data.CategoryName);
        Assert.True(detail.Data.PaymentRequired);
        Assert.Equal("Scheduled", detail.Data.Status);
    }

    [Theory]
    [InlineData(EventStatus.Scheduled, 9, true)]
    [InlineData(EventStatus.Scheduled, 10, false)]
    [InlineData(EventStatus.Completed, 0, false)]
    [InlineData(EventStatus.Canceled, 0, false)]
    public async Task Availability_ProjectsStatusAndRemainingCapacity(EventStatus status, int count, bool available)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(count: count);
        await fixture.Db.Events.ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, status));

        // Act
        var result = await fixture.Mediator.Send(new CheckEventAvailabilityQuery(entity.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(available, result.Data!.IsAvailable);
        Assert.Equal(10 - count, result.Data.RemainingSlots);
        Assert.Equal(status == EventStatus.Canceled, result.Data.IsCancelled);
    }

    [Theory]
    [InlineData(RoleNames.Admin, false, true)]
    [InlineData(RoleNames.Organizer, true, true)]
    [InlineData(RoleNames.Organizer, false, false)]
    [InlineData(RoleNames.Attendee, true, false)]
    public async Task ManagedEvent_DoesNotExposeAnotherOrganizersEvent(string role, bool owner, bool allowed)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        fixture.User.Role = role;
        fixture.User.UserId = owner ? WorkflowFixture.OwnerId : WorkflowFixture.OtherId;

        // Act
        var result = await fixture.Mediator.Send(new GetManagedEventByIdQuery(entity.Id));

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed) Assert.Equal(role == RoleNames.Attendee ? ErrorCode.Forbidden : ErrorCode.EventNotFound, result.ErrorCode);
    }

    [Theory]
    [InlineData(RegistrationStatus.Confirmed, EventMode.Online, true, true)]
    [InlineData(RegistrationStatus.Pending, EventMode.Online, true, false)]
    [InlineData(RegistrationStatus.Canceled, EventMode.Online, true, false)]
    [InlineData(RegistrationStatus.Confirmed, EventMode.Offline, true, false)]
    [InlineData(RegistrationStatus.Confirmed, EventMode.Online, false, false)]
    public async Task MeetingLink_RequiresOwnConfirmedOnlineRegistration(RegistrationStatus status, EventMode mode, bool own, bool allowed)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        await fixture.Db.Events.ExecuteUpdateAsync(s => s.SetProperty(e => e.Mode, mode).SetProperty(e => e.OnlineMeetingUrl, "https://meet.example.test/room"));
        await fixture.SeedRegistrationAsync(entity, status, own ? WorkflowFixture.AttendeeId : WorkflowFixture.OtherId);

        // Act
        var result = await fixture.Mediator.Send(new GetMyEventMeetingLinkQuery(entity.Id));

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        if (allowed) Assert.Equal("https://meet.example.test/room", result.Data!.OnlineMeetingUrl);
        else Assert.Equal(ErrorCode.RegistrationNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task RegistrationReads_RestrictOwnershipAndProjectLatestPayment()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100);
        var own = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Pending);
        var other = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Confirmed, WorkflowFixture.OtherId);
        await fixture.SeedPaymentAsync(own, PaymentTransactionStatus.Success);

        // Act
        var mine = await fixture.Mediator.Send(new GetMyRegistrationsQuery());
        var status = await fixture.Mediator.Send(new GetMyRegistrationStatusQuery(own.Id));
        var denied = await fixture.Mediator.Send(new GetMyRegistrationStatusQuery(other.Id));
        fixture.User.Role = RoleNames.Admin;
        var all = await fixture.Mediator.Send(new GetAllRegistrationsQuery());
        var attendees = await fixture.Mediator.Send(new GetEventRegistrationsQuery(entity.Id));

        // Assert
        Assert.Equal(own.Id, Assert.Single(mine.Data!.Items).Id);
        Assert.Equal(PaymentTransactionStatus.Success, status.Data!.PaymentStatus);
        Assert.Equal(100m, status.Data.PaymentAmount);
        Assert.Equal(ErrorCode.RegistrationNotFound, denied.ErrorCode);
        Assert.Equal(2, all.Data!.TotalCount);
        Assert.Equal(2, attendees.Data!.TotalCount);
        Assert.Contains(attendees.Data.Items, r => r.AttendeeEmail == "attendee@example.test");
    }

    [Fact]
    public async Task OrganizerQueriesAndDashboard_ExcludeOtherOwners()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var own = await fixture.SeedEventAsync(count: 3);
        var other = await fixture.SeedEventAsync(count: 7);
        await fixture.Db.Events.Where(e => e.Id == other.Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.OrganizerId, WorkflowFixture.OtherId));
        await fixture.SeedRegistrationAsync(own);
        await fixture.SeedRegistrationAsync(other);
        fixture.User.Role = RoleNames.Organizer;
        fixture.User.UserId = WorkflowFixture.OwnerId;

        // Act
        var events = await fixture.Mediator.Send(new GetMyEventsQuery(null, null, null));
        var dashboard = await fixture.Mediator.Send(new GetOrganizerDashboardQuery());

        // Assert
        Assert.Equal(own.Id, Assert.Single(events.Data!.Items).Id);
        Assert.Equal(1, dashboard.Data!.TotalEvents);
        Assert.Equal(1, dashboard.Data.TotalRegistrations);
        Assert.Equal(3, dashboard.Data.TotalAttendees);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdminDashboard_AggregatesOnlySuccessfulRevenueAndHandlesEmptyData(bool seed)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        fixture.User.Role = RoleNames.Admin;
        fixture.Users.Setup(u => u.GetUserCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        if (seed)
        {
            var entity = await fixture.SeedEventAsync();
            var first = await fixture.SeedRegistrationAsync(entity);
            var second = await fixture.SeedRegistrationAsync(entity, userId: WorkflowFixture.OtherId);
            await fixture.SeedPaymentAsync(first, PaymentTransactionStatus.Success);
            await fixture.SeedPaymentAsync(second, PaymentTransactionStatus.Failed);
        }

        // Act
        var result = await fixture.Mediator.Send(new GetAdminDashboardQuery());

        // Assert
        Assert.Equal(3, result.Data!.TotalUsers);
        Assert.Equal(seed ? 1 : 0, result.Data.TotalEvents);
        Assert.Equal(seed ? 2 : 0, result.Data.TotalRegistrations);
        Assert.Equal(seed ? 100m : 0m, result.Data.Payments.SuccessfulPaymentAmount);
        Assert.Equal(seed ? 1 : 0, result.Data.Payments.FailedOrCanceledPayments);
    }
}
