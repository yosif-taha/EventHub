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
public class AuthContractTests
{

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new LoginQuery("user@example.test", "password");
        fixture.Mediator.Setup(m => m.Send(It.Is<LoginQuery>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<AuthResponse>.Success(new("id", "user@example.test", "User", "token", 60, "refresh", WorkflowFixture.Future)) : RequestResult<AuthResponse>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Auth/Login", new { email = "user@example.test", password = "password" });
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
    public async Task Register_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new RegisterCommand("user@example.test", "password", "User", null);
        fixture.Mediator.Setup(m => m.Send(It.Is<RegisterCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<Guid>.Success(WorkflowFixture.AttendeeId) : RequestResult<Guid>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Auth/Register", new { email = "user@example.test", password = "password", fullName = "User" });
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
    public async Task Confirm_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new ConfirmEmailCommand("id", "code");
        fixture.Mediator.Setup(m => m.Send(It.Is<ConfirmEmailCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Auth/ConfirmEmail", new { userId = "id", code = "code" });
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
    public async Task Reset_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new ResetPasswordCommand("user@example.test", "code", "password");
        fixture.Mediator.Setup(m => m.Send(It.Is<ResetPasswordCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Auth/ResetPassword", new { email = "user@example.test", code = "code", newPassword = "password" });
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
    public async Task Refresh_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new RefreshTokenCommand("token", "refresh");
        fixture.Mediator.Setup(m => m.Send(It.Is<RefreshTokenCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<AuthResponse>.Success(new("id", "user@example.test", "User", "token", 60, "refresh", WorkflowFixture.Future)) : RequestResult<AuthResponse>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Auth/RefreshToken", new { token = "token", refreshToken = "refresh" });
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
    public async Task Resend_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new ResendConfirmationEmailCommand("user@example.test");
        fixture.Mediator.Setup(m => m.Send(It.Is<ResendConfirmationEmailCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Auth/ResendConfirmationEmail", new { email = "user@example.test" });
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
    public async Task SendReset_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new SendResetPasswordCommand("user@example.test");
        fixture.Mediator.Setup(m => m.Send(It.Is<SendResetPasswordCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Auth/SendResetPassword", new { email = "user@example.test" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.ValidationError, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

}
