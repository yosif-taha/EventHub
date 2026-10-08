using EventHub.Domin.Enums;
using EventHub.MVC.Controllers;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Admins;
using EventHub.MVC.Models.Organizers;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Events;
using EventHub.MVC.Services.Admins;
using EventHub.MVC.Services.Organizers;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EventHub.Tests.MVC;
[Trait("Category", "MockUnit")]
public class AdminReadWorkflowTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Dashboard_PresentsDataOrAnExplicitLoadError(bool success)
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        var data = new AdminDashboardDto { TotalEvents = 3, TotalRegistrations = 5 };
        api.Setup(a => a.GetDashboardAsync(default)).ReturnsAsync(success
            ? ApiCallResult<AdminDashboardDto>.Success(data)
            : ApiCallResult<AdminDashboardDto>.Failure("Unavailable", ApiFailureKind.Unavailable));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var model = Assert.IsType<AdminDashboardViewModel>(Assert.IsType<ViewResult>(await controller.Dashboard(default)).Model);

        // Assert
        if (success) { Assert.Same(data, model.Dashboard); Assert.Null(model.ErrorMessage); }
        else { Assert.Equal("Unavailable", model.ErrorMessage); Assert.Equal(0, model.Dashboard.TotalEvents); }
    }

    [Theory]
    [InlineData("Title", "Title")]
    [InlineData("invalid", null)]
    [InlineData("EventDate", null)]
    public async Task Events_NormalizesFilterAndPreservesPagination(string column, string? expectedColumn)
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        var events = new Mock<IEventApiClient>();
        var page = new PaginatedResult<EventSummaryDto> { TotalCount = 9 };
        api.Setup(a => a.GetEventsAsync(It.IsAny<EventListFilter>(), default)).ReturnsAsync(ApiCallResult<PaginatedResult<EventSummaryDto>>.Success(page));
        events.Setup(e => e.GetCategoriesAsync(default)).ReturnsAsync(ApiCallResult<List<CategoryDto>>.Success([]));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, events.Object));
        var filter = new EventListFilter { PageNumber = -1, PageSize = 100, SortColumn = column, SortDirection = "DESC" };

        // Act
        var model = Assert.IsType<AdminEventListViewModel>(Assert.IsType<ViewResult>(await controller.Events(filter, default)).Model);

        // Assert
        Assert.Same(page, model.Events);
        Assert.Equal(1, model.Filter.PageNumber); Assert.Equal(10, model.Filter.PageSize);
        Assert.Equal(expectedColumn, model.Filter.SortColumn); Assert.Equal("desc", model.Filter.SortDirection);
        api.Verify(a => a.GetEventsAsync(filter, default), Times.Once);
    }

    [Fact]
    public async Task Edit_LoadsExistingFieldsAndCategories()
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        var events = new Mock<IEventApiClient>();
        var detail = new EventDetailsDto { Title = "Conference", Description = "Description", Location = "Cairo", EventDate = WorkflowFixture.Future,
            MaxAttendees = 20, Mode = EventMode.Online, OnlineMeetingUrl = "https://meet.example.test/room", Price = 25, CategoryName = "Science", Status = "Scheduled" };
        api.Setup(a => a.GetEventAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(ApiCallResult<EventDetailsDto>.Success(detail));
        events.Setup(e => e.GetCategoriesAsync(default)).ReturnsAsync(ApiCallResult<List<CategoryDto>>.Failure("Categories unavailable", ApiFailureKind.Unavailable));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, events.Object));

        // Act
        var model = Assert.IsType<EventEditorViewModel>(Assert.IsType<ViewResult>(await controller.Edit(WorkflowFixture.OtherId, default)).Model);

        // Assert
        Assert.True(model.IsEdit); Assert.Equal(detail.Title, model.Title); Assert.Equal(detail.EventDate, model.EventDate);
        Assert.Equal(DateTimeKind.Utc, model.EventDate.Kind); Assert.Equal(detail.OnlineMeetingUrl, model.OnlineMeetingUrl);
        Assert.Equal(25, model.Price); Assert.Equal("Science", model.CurrentCategoryName);
        Assert.Empty(model.Categories); Assert.Equal("Categories unavailable", model.CategoryLoadError);
    }

    [Theory]
    [InlineData(ApiFailureKind.NotFound)]
    [InlineData(ApiFailureKind.Forbidden)]
    [InlineData(ApiFailureKind.Unavailable)]
    public async Task EditReadFailure_MapsToAppropriateMvcResult(ApiFailureKind failure)
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.GetEventAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(ApiCallResult<EventDetailsDto>.Failure("Rejected", failure));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var result = await controller.Edit(WorkflowFixture.OtherId, default);

        // Assert
        if (failure == ApiFailureKind.NotFound) Assert.IsType<NotFoundResult>(result);
        else if (failure == ApiFailureKind.Forbidden) Assert.IsType<ForbidResult>(result);
        else Assert.Equal("~/Views/Home/Error.cshtml", Assert.IsType<ViewResult>(result).ViewName);
    }

    [Fact]
    public async Task Attendees_ClampsPageAndUsesFallbackTitleWhenEventReadFails()
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        var page = new PaginatedResult<AdminEventRegistrationDto> { TotalCount = 7 };
        api.Setup(a => a.GetEventRegistrationsAsync(WorkflowFixture.OtherId, 1, 10, default)).ReturnsAsync(ApiCallResult<PaginatedResult<AdminEventRegistrationDto>>.Success(page));
        api.Setup(a => a.GetEventAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(ApiCallResult<EventDetailsDto>.Failure("Unavailable", ApiFailureKind.Unavailable));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var model = Assert.IsType<AdminAttendeesViewModel>(Assert.IsType<ViewResult>(await controller.Attendees(WorkflowFixture.OtherId, -1)).Model);

        // Assert
        Assert.Equal(WorkflowFixture.OtherId, model.EventId); Assert.Equal("Event attendees", model.EventTitle);
        Assert.Same(page, model.Attendees); api.VerifyAll();
    }

    [Fact]
    public async Task Announcement_LoadsAudienceAndRestoresContextOnValidationFailure()
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.GetEventRegistrationsAsync(WorkflowFixture.OtherId, 1, 1, default)).ReturnsAsync(ApiCallResult<PaginatedResult<AdminEventRegistrationDto>>.Success(new() { TotalCount = 9 }));
        api.Setup(a => a.GetEventAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(ApiCallResult<EventDetailsDto>.Success(new() { Title = "Conference" }));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var initial = Assert.IsType<EventAnnouncementViewModel>(Assert.IsType<ViewResult>(await controller.Announcement(WorkflowFixture.OtherId, default)).Model);
        controller.ModelState.AddModelError("Subject", "Required");
        var submitted = new EventAnnouncementViewModel { EventId = WorkflowFixture.AttendeeId, Message = "Keep my message" };
        var result = Assert.IsType<ViewResult>(await controller.Announcement(WorkflowFixture.OtherId, submitted, default));

        // Assert
        Assert.Equal(9, initial.RegistrationCount); Assert.Equal("Conference", initial.EventTitle);
        Assert.Same(submitted, result.Model); Assert.Equal("Announcement", result.ViewName);
        Assert.Equal(WorkflowFixture.OtherId, submitted.EventId); Assert.Equal("Keep my message", submitted.Message);
        Assert.Equal(9, submitted.RegistrationCount); Assert.Equal("Conference", submitted.EventTitle);
        api.Verify(a => a.SendAnnouncementAsync(It.IsAny<SendEventAnnouncementRequest>(), default), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cancel_RequestsCanceledStateAndReportsOutcome(bool success)
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.UpdateEventStatusAsync(WorkflowFixture.OtherId, EventStatus.Canceled, default))
            .ReturnsAsync(success ? ApiCallResult<object>.Success(new()) : ApiCallResult<object>.Failure("Rejected", ApiFailureKind.Validation));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var result = await controller.Cancel(WorkflowFixture.OtherId, default);

        // Assert
        Assert.Equal("Events", Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.True(controller.TempData.ContainsKey(success ? "Success" : "Error"));
        if (!success) Assert.Equal("Rejected", controller.TempData["Error"]);
        api.VerifyAll();
    }
}
