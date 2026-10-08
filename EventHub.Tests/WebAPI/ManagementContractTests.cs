using EventHub.Application.Common.Responses;
using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Dtos.Category;
using EventHub.Application.Common.Dtos.Dashboards;
using EventHub.Application.Features.Auth.Login;
using EventHub.Application.Features.Auth.Register;
using EventHub.Application.Features.Auth.ConfirmEmail;
using EventHub.Application.Features.Auth.PasswordReset;
using EventHub.Application.Features.Auth.RefreshTokens;
using EventHub.Application.Features.Account.GetUserProfile;
using EventHub.Application.Features.Account.UpdateUserProfile;
using EventHub.Application.Features.Account.ChangePassword;
using EventHub.Application.Features.Category.Create_Category;
using EventHub.Application.Features.Category.Update_Category;
using EventHub.Application.Features.Category.Delete_Category;
using EventHub.Application.Features.Category.Get_Category_By_Id;
using EventHub.Application.Features.Category.Get_All_Categories;
using EventHub.Application.Features.Dashboards.GetAdminDashboard;
using EventHub.Application.Features.Dashboards.GetOrganizerDashboard;
using EventHub.Application.Features.Notifications.SendEventAnnouncement;
using EventHub.Application.Features.Admin.UpdateUserRole;
using EventHub.Domin.Enums;
using EventHub.Tests.Support;
using MediatR;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EventHub.Tests.WebAPI;
[Trait("Category", "Http")]
public class ManagementContractTests
{

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdminDashboard_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Admin");
        var expected = new GetAdminDashboardQuery();
        fixture.Mediator.Setup(m => m.Send(It.Is<GetAdminDashboardQuery>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<AdminDashboardDto>.Success(new() { TotalEvents = 2 }) : RequestResult<AdminDashboardDto>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.GetAsync("/api/admin/dashboard");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.ValidationError, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OrganizerDashboard_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Organizer");
        var expected = new GetOrganizerDashboardQuery();
        fixture.Mediator.Setup(m => m.Send(It.Is<GetOrganizerDashboardQuery>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<OrganizerDashboardDto>.Success(new() { TotalEvents = 2 }) : RequestResult<OrganizerDashboardDto>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.GetAsync("/api/organizer/dashboard");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.ValidationError, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Announcement_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Organizer");
        var expected = new SendEventAnnouncementCommand(WorkflowFixture.AttendeeId, "Subject", "Message");
        fixture.Mediator.Setup(m => m.Send(It.Is<SendEventAnnouncementCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<int>.Success(2) : RequestResult<int>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/notifications", new { eventId = WorkflowFixture.AttendeeId, subject = "Subject", message = "Message" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.ValidationError, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Role_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Admin");
        var expected = new UpdateUserRoleCommand(WorkflowFixture.AttendeeId, UserRole.Organizer);
        fixture.Mediator.Setup(m => m.Send(It.Is<UpdateUserRoleCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Admin/UpdateUserRole/22222222-2222-2222-2222-222222222222", new { role = UserRole.Organizer });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.ValidationError, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

}
