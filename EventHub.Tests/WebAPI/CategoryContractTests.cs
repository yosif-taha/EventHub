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
public class CategoryContractTests
{

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Admin");
        var expected = new CreateCategoryCommand("Science");
        fixture.Mediator.Setup(m => m.Send(It.Is<CreateCategoryCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<Guid>.Success(WorkflowFixture.AttendeeId) : RequestResult<Guid>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Category/CreateCategory", new { name = "Science" });
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
    public async Task Update_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Admin");
        var expected = new UpdateCategoryCommand(WorkflowFixture.AttendeeId, "Science");
        fixture.Mediator.Setup(m => m.Send(It.Is<UpdateCategoryCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<Unit>.Success(Unit.Value) : RequestResult<Unit>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Category/UpdateCategory", new { id = WorkflowFixture.AttendeeId, name = "Science" });
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
    public async Task Delete_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Admin");
        var expected = new DeleteCategoryCommand(WorkflowFixture.AttendeeId);
        fixture.Mediator.Setup(m => m.Send(It.Is<DeleteCategoryCommand>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<Unit>.Success(Unit.Value) : RequestResult<Unit>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.PostAsJsonAsync<object?>("/api/Category/DeleteCategory?id=22222222-2222-2222-2222-222222222222", null);
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
    public async Task Get_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new GetCategoryByIdQuery(WorkflowFixture.AttendeeId);
        fixture.Mediator.Setup(m => m.Send(It.Is<GetCategoryByIdQuery>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<CategoryDto>.Success(new(WorkflowFixture.AttendeeId, "Science", 2)) : RequestResult<CategoryDto>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.GetAsync("/api/Category/GetCategoryById?id=22222222-2222-2222-2222-222222222222");
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
    public async Task List_BindsRequestAndReturnsSuccessOrBusinessFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        
        var expected = new GetAllCategoriesQuery();
        fixture.Mediator.Setup(m => m.Send(It.Is<GetAllCategoriesQuery>(r => r == expected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<IEnumerable<CategoryDto>>.Success([new(WorkflowFixture.AttendeeId, "Science", 2)]) : RequestResult<IEnumerable<CategoryDto>>.Failure(ErrorCode.ValidationError));

        // Act
        var response = await fixture.Client.GetAsync("/api/Category/GetAllCategories");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.ValidationError, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

}
