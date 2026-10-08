using EventHub.Domin.Constants;
using EventHub.MVC.Controllers;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Registrations;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Events;
using EventHub.MVC.Services.Registrations;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class EventsControllerTests
{
    [Theory]
    [InlineData(-1, 0, "bad", "bad", 1, 10, null, "asc")]
    [InlineData(2, 50, "Title", "DESC", 2, 50, "Title", "desc")]
    [InlineData(1, 51, "Location", "asc", 1, 10, "Location", "asc")]
    public async Task List_NormalizesPaginationAndSortBeforeCallingBackend(int page, int size, string column, string direction, int expectedPage, int expectedSize, string? expectedColumn, string expectedDirection)
    {
        // Arrange
        var api = new Mock<IEventApiClient>();
        api.Setup(a => a.GetEventsAsync(It.IsAny<EventListFilter>(), default)).ReturnsAsync(ApiCallResult<PaginatedResult<EventSummaryDto>>.Failure("Unavailable", ApiFailureKind.Unavailable));
        api.Setup(a => a.GetCategoriesAsync(default)).ReturnsAsync(ApiCallResult<List<CategoryDto>>.Success([]));
        var controller = ManagementControllerTests.Prepare(new EventsController(api.Object, Mock.Of<IRegistrationApiClient>()));
        var filter = new EventListFilter { PageNumber = page, PageSize = size, SortColumn = column, SortDirection = direction };

        // Act
        var view = Assert.IsType<ViewResult>(await controller.Index(filter, default));

        // Assert
        var model = Assert.IsType<EventListViewModel>(view.Model);
        Assert.Equal("Unavailable", model.ErrorMessage);
        api.Verify(a => a.GetEventsAsync(It.Is<EventListFilter>(f => f.PageNumber == expectedPage && f.PageSize == expectedSize && f.SortColumn == expectedColumn && f.SortDirection == expectedDirection), default), Times.Once);
    }

    [Theory]
    [InlineData(RoleNames.Attendee, true)]
    [InlineData(RoleNames.Organizer, false)]
    [InlineData("", false)]
    public async Task Details_LoadsPrivateMeetingLinkOnlyForAttendeesAndUsesSafeAvailabilityFallback(string role, bool loadsLink)
    {
        // Arrange
        var api = new Mock<IEventApiClient>();
        var registrations = new Mock<IRegistrationApiClient>();
        api.Setup(a => a.GetEventAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(ApiCallResult<EventDetailsDto>.Success(new() { Status = "Canceled", RemainingSlots = 3 }));
        api.Setup(a => a.GetAvailabilityAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(ApiCallResult<EventAvailabilityDto>.Failure("Unavailable", ApiFailureKind.Unavailable));
        registrations.Setup(r => r.GetMeetingLinkAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(ApiCallResult<EventMeetingLinkDto>.Failure("Not registered", ApiFailureKind.NotFound));
        var controller = ManagementControllerTests.Prepare(new EventsController(api.Object, registrations.Object));
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

        // Act
        var model = Assert.IsType<EventDetailsViewModel>(Assert.IsType<ViewResult>(await controller.Details(WorkflowFixture.OtherId, default)).Model);

        // Assert
        Assert.False(model.Availability.IsAvailable);
        Assert.True(model.Availability.IsCancelled);
        Assert.Equal(3, model.Availability.RemainingSlots);
        Assert.Null(model.AttendeeMeetingLink);
        registrations.Verify(r => r.GetMeetingLinkAsync(WorkflowFixture.OtherId, default), Times.Exactly(loadsLink ? 1 : 0));
    }
}
