using EventHub.Domin.Enums;
using EventHub.MVC.Controllers;
using EventHub.MVC.Models.Admins;
using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Organizers;
using EventHub.MVC.Services.Admins;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Events;
using EventHub.MVC.Services.Organizers;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class AdminOperationsTests
{
    [Theory]
    [InlineData("  Alice  ", "Alice")]
    [InlineData("  ", null)]
    public async Task Users_NormalizesSearchAndRetainsServiceError(string search, string? expected)
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.GetUsersAsync(It.IsAny<AdminUserListFilter>(), default))
            .ReturnsAsync(ApiCallResult<PaginatedResult<AdminUserDto>>.Failure("Unavailable", ApiFailureKind.Unavailable));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));
        var filter = new AdminUserListFilter { PageNumber = -5, SearchValue = search };

        // Act
        var model = Assert.IsType<AdminUsersViewModel>(Assert.IsType<ViewResult>(await controller.Users(filter, default)).Model);

        // Assert
        Assert.Equal(1, model.Filter.PageNumber); Assert.Equal(expected, model.Filter.SearchValue);
        Assert.Equal("Unavailable", model.ErrorMessage); Assert.Empty(model.Users.Items);
        api.Verify(a => a.GetUsersAsync(filter, default), Times.Once);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("failure")]
    [InlineData("success")]
    public async Task RoleUpdate_RespectsModelValidationAndReportsBackendOutcome(string scenario)
    {
        // Arrange
        var api = new Mock<IAdminApiClient>(MockBehavior.Strict);
        if (scenario != "invalid")
            api.Setup(a => a.UpdateUserRoleAsync(WorkflowFixture.OtherId, UserRole.Organizer, default)).ReturnsAsync(scenario == "success"
                ? ApiCallResult<object>.Success(new()) : ApiCallResult<object>.Failure("Last administrator", ApiFailureKind.Validation));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));
        if (scenario == "invalid") controller.ModelState.AddModelError("Role", "Invalid role");

        // Act
        var result = await controller.UpdateUserRole(new() { UserId = WorkflowFixture.OtherId, Role = UserRole.Organizer }, default);

        // Assert
        Assert.Equal("Users", Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.True(controller.TempData.ContainsKey(scenario == "success" ? "Success" : "Error"));
        if (scenario == "invalid") api.VerifyNoOtherCalls(); else api.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ReportsServiceOutcome(bool success)
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.DeleteEventAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(success
            ? ApiCallResult<bool>.Success(true) : ApiCallResult<bool>.Failure("Not found", ApiFailureKind.NotFound));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var result = await controller.Delete(WorkflowFixture.OtherId, default);

        // Assert
        Assert.Equal("Events", Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.True(controller.TempData.ContainsKey(success ? "Success" : "Error"));
        api.VerifyAll();
    }

    [Fact]
    public async Task Registrations_ClampsPageAndPreservesResult()
    {
        // Arrange
        var api = new Mock<IAdminApiClient>();
        var data = new PaginatedResult<AdminRegistrationDto> { TotalCount = 3 };
        api.Setup(a => a.GetRegistrationsAsync(1, 25, default)).ReturnsAsync(ApiCallResult<PaginatedResult<AdminRegistrationDto>>.Success(data));
        var controller = ManagementControllerTests.Prepare(new AdminController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var model = Assert.IsType<AdminRegistrationsViewModel>(Assert.IsType<ViewResult>(await controller.Registrations(-1)).Model);

        // Assert
        Assert.Same(data, model.Registrations); Assert.Null(model.ErrorMessage); api.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OrganizerStatistics_UsesTotalCountRatherThanSinglePageSize(bool available)
    {
        // Arrange
        var api = new Mock<IOrganizerApiClient>();
        api.Setup(a => a.GetAttendeesAsync(WorkflowFixture.OtherId, 1, 1, default))
            .ReturnsAsync(ApiCallResult<PaginatedResult<EventRegistrationDto>>.Success(new() { TotalCount = 25, Items = [new()] }));
        api.Setup(a => a.GetEventAsync(WorkflowFixture.OtherId, default)).ReturnsAsync(available
            ? ApiCallResult<EventDetailsDto>.Success(new() { Title = "Conference" })
            : ApiCallResult<EventDetailsDto>.Failure("Not found", ApiFailureKind.NotFound));
        var controller = ManagementControllerTests.Prepare(new OrganizerController(api.Object, Mock.Of<IEventApiClient>()));

        // Act
        var result = await controller.Statistics(WorkflowFixture.OtherId, default);

        // Assert
        if (available) {
            var model = Assert.IsType<OrganizerEventStatisticsViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(25, model.RegistrationCount); Assert.Equal("Conference", model.Event.Title);
        }
        else Assert.IsType<NotFoundResult>(result);
        api.VerifyAll();
    }
}
