using EventHub.Domin.Enums;
using EventHub.MVC.Controllers;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Organizers;
using EventHub.MVC.Models.Admins;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Events;
using EventHub.MVC.Services.Organizers;
using EventHub.MVC.Services.Admins;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class ManagementControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_MapsUtcAndTrimmedUrlForBothManagementControllers(bool admin)
    {
        // Arrange
        var events = new Mock<IEventApiClient>(MockBehavior.Strict);
        var admins = new Mock<IAdminApiClient>(MockBehavior.Strict);
        var organizers = new Mock<IOrganizerApiClient>(MockBehavior.Strict);
        CreateOrganizerEventRequest? captured = null;
        if (admin)
            admins.Setup(a => a.CreateEventAsync(It.IsAny<CreateOrganizerEventRequest>(), default))
                .Callback<CreateOrganizerEventRequest, CancellationToken>((r, _) => captured = r).ReturnsAsync(ApiCallResult<Guid>.Success(WorkflowFixture.OwnerId));
        else
            organizers.Setup(a => a.CreateEventAsync(It.IsAny<CreateOrganizerEventRequest>(), default))
                .Callback<CreateOrganizerEventRequest, CancellationToken>((r, _) => captured = r).ReturnsAsync(ApiCallResult<Guid>.Success(WorkflowFixture.OwnerId));
        var model = EventEditorValidationTests.Valid();
        model.EventDate = DateTime.SpecifyKind(model.EventDate, DateTimeKind.Unspecified);
        model.Mode = EventMode.Online; model.OnlineMeetingUrl = "  https://meet.example.test/room  ";
        var adminController = Prepare(new AdminController(admins.Object, events.Object));
        var organizerController = Prepare(new OrganizerController(organizers.Object, events.Object));

        // Act
        var result = admin ? await adminController.Create(model, default) : await organizerController.Create(model, default);

        // Assert
        Assert.Equal("Events", Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.NotNull(captured);
        Assert.Equal(DateTimeKind.Utc, captured.EventDate.Kind);
        Assert.Equal(model.EventDate.Ticks, captured.EventDate.Ticks);
        Assert.Equal("https://meet.example.test/room", captured.OnlineMeetingUrl);
        Assert.Equal(model.CategoryId, captured.CategoryId);
        admins.VerifyAll(); organizers.VerifyAll(); events.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_MissingCategoryRedisplaysInputAndDoesNotWrite(bool admin)
    {
        // Arrange
        var events = new Mock<IEventApiClient>();
        events.Setup(e => e.GetCategoriesAsync(default)).ReturnsAsync(ApiCallResult<List<CategoryDto>>.Success([]));
        var admins = new Mock<IAdminApiClient>(MockBehavior.Strict);
        var organizers = new Mock<IOrganizerApiClient>(MockBehavior.Strict);
        var model = EventEditorValidationTests.Valid(); model.CategoryId = null;
        Controller controller = admin ? Prepare(new AdminController(admins.Object, events.Object)) : Prepare(new OrganizerController(organizers.Object, events.Object));

        // Act
        var result = controller is AdminController a ? await a.Create(model, default) : await ((OrganizerController)controller).Create(model, default);

        // Assert
        Assert.Same(model, Assert.IsType<ViewResult>(result).Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.CategoryId)));
        admins.VerifyNoOtherCalls(); organizers.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, ApiFailureKind.Forbidden)]
    [InlineData(true, ApiFailureKind.Unauthorized)]
    [InlineData(false, ApiFailureKind.Validation)]
    [InlineData(true, ApiFailureKind.Validation)]
    public async Task Edit_HandlesAuthorizationAndValidationFailures(bool admin, ApiFailureKind failure)
    {
        // Arrange
        var events = new Mock<IEventApiClient>();
        events.Setup(e => e.GetCategoriesAsync(default)).ReturnsAsync(ApiCallResult<List<CategoryDto>>.Success([]));
        var admins = new Mock<IAdminApiClient>();
        var organizers = new Mock<IOrganizerApiClient>();
        admins.Setup(a => a.UpdateEventAsync(WorkflowFixture.OtherId, It.IsAny<UpdateOrganizerEventRequest>(), default)).ReturnsAsync(ApiCallResult<object>.Failure("Rejected", failure));
        organizers.Setup(a => a.UpdateEventAsync(WorkflowFixture.OtherId, It.IsAny<UpdateOrganizerEventRequest>(), default)).ReturnsAsync(ApiCallResult<object>.Failure("Rejected", failure));
        var model = EventEditorValidationTests.Valid();
        Controller controller = admin ? Prepare(new AdminController(admins.Object, events.Object)) : Prepare(new OrganizerController(organizers.Object, events.Object));

        // Act
        var result = controller is AdminController a ? await a.Edit(WorkflowFixture.OtherId, model, default) : await ((OrganizerController)controller).Edit(WorkflowFixture.OtherId, model, default);

        // Assert
        if (failure == ApiFailureKind.Validation) { Assert.Same(model, Assert.IsType<ViewResult>(result).Model); Assert.False(controller.ModelState.IsValid); }
        else Assert.IsType<ForbidResult>(result);
        Assert.True(model.IsEdit);
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Canceled")]
    public async Task AdminEdit_RefusesTerminalEvents(string status)
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.GetEventAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(ApiCallResult<EventDetailsDto>.Success(new() { Status = status }));
        var controller = Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var result = await controller.Edit(WorkflowFixture.OtherId, default);

        // Assert
        Assert.Equal("Events", Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.Contains("scheduled", controller.TempData["Error"]!.ToString());
    }

    [Fact]
    public async Task Announcement_UsesRouteIdInsteadOfPostedId()
    {
        // Arrange
        var api = new Mock<IOrganizerApiClient>(MockBehavior.Strict);
        api.Setup(a => a.SendAnnouncementAsync(It.Is<SendEventAnnouncementRequest>(r =>
            r.EventId == WorkflowFixture.OtherId && r.Subject == "Subject" && r.Message == "Message"), default)).ReturnsAsync(ApiCallResult<object>.Success(new()));
        var controller = Prepare(new OrganizerController(api.Object, Mock.Of<IEventApiClient>()));
        var model = new EventAnnouncementViewModel { EventId = WorkflowFixture.AttendeeId, Subject = "Subject", Message = "Message" };

        // Act
        var result = await controller.Announcement(WorkflowFixture.OtherId, model, default);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Attendees", redirect.ActionName);
        Assert.Equal(WorkflowFixture.OtherId, redirect.RouteValues!["id"]);
        api.VerifyAll();
    }

    internal static T Prepare<T>(T controller) where T : Controller
    {
        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        controller.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
        return controller;
    }
}
