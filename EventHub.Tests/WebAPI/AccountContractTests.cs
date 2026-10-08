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
public class AccountContractTests
{

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetProfile_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Attendee");
        var expected = new GetUserProfileQuery();
        fixture.Mediator.Setup(m => m.Send(It.Is<GetUserProfileQuery>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<UserProfileResponse>.Success(new("user@example.test", "user", "User", null)) : RequestResult<UserProfileResponse>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.GetAsync("/api/Account/GetUserProfile");
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
    public async Task UpdateProfile_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Attendee");
        var expected = new UpdateUserProfileCommand("Updated User", null);
        fixture.Mediator.Setup(m => m.Send(It.Is<UpdateUserProfileCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Account/UpdateUserProfile", new { fullName = "Updated User" });
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
    public async Task ChangePassword_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Attendee");
        var expected = new ChangePasswordCommand("old", "new");
        fixture.Mediator.Setup(m => m.Send(It.Is<ChangePasswordCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Account/ChangePassword", new { currentPassword = "old", newPassword = "new" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.ValidationError, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

}
