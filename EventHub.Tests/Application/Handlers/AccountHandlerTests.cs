using EventHub.Application.Contracts;
using EventHub.Application.Common.Responses;
using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Features.Account.GetUserProfile;
using EventHub.Application.Features.Account.UpdateUserProfile;
using EventHub.Application.Features.Account.ChangePassword;
using EventHub.Application.Features.Admin.UpdateUserRole;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Tests.Support;
using Moq;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "MockUnit")]
public class AccountHandlerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AccountOperations_UseCurrentUserAndPropagateFailures(bool success)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var user = new TestUserContext();
        var service = new Mock<IAccountService>(MockBehavior.Strict);
        var profile = new UserProfileResponse("user@example.test", "user", "Test User", null);
        var boolean = success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.UserNotFound);
        service.Setup(s => s.GetUserProfileAsync(user.UserId.ToString(), cancellation.Token))
            .ReturnsAsync(success ? RequestResult<UserProfileResponse>.Success(profile) : RequestResult<UserProfileResponse>.Failure(ErrorCode.UserNotFound));
        service.Setup(s => s.UpdateUserProfileAsync(user.UserId.ToString(), "Test User", null, cancellation.Token)).ReturnsAsync(boolean);
        service.Setup(s => s.ChangePasswordAsync(user.UserId.ToString(), "old", "new", cancellation.Token)).ReturnsAsync(boolean);

        // Act
        var get = await new GetUserProfileQueryHandler(service.Object, user).Handle(new GetUserProfileQuery(), cancellation.Token);
        var update = await new UpdateUserProfileCommandHandler(service.Object, user).Handle(new UpdateUserProfileCommand("Test User", null), cancellation.Token);
        var password = await new ChangePasswordCommandHandler(service.Object, user).Handle(new ChangePasswordCommand("old", "new"), cancellation.Token);

        // Assert
        Assert.Equal(success, get.IsSuccess); Assert.Equal(success, update.IsSuccess); Assert.Equal(success, password.IsSuccess);
        if (success) Assert.Same(profile, get.Data);
        else { Assert.Equal(ErrorCode.UserNotFound, get.ErrorCode); Assert.Equal(ErrorCode.UserNotFound, update.ErrorCode); Assert.Equal(ErrorCode.UserNotFound, password.ErrorCode); }
        service.VerifyAll();
    }

    [Theory]
    [InlineData(RoleNames.Attendee, false, UserRole.Admin, ErrorCode.Forbidden)]
    [InlineData(RoleNames.Organizer, false, UserRole.Admin, ErrorCode.Forbidden)]
    [InlineData(RoleNames.Admin, true, UserRole.Attendee, ErrorCode.ValidationError)]
    public async Task RoleChange_RejectsUnauthorizedOrSelfDemotionWithoutCallingService(string role, bool self, UserRole requested, ErrorCode expected)
    {
        // Arrange
        var user = new TestUserContext { Role = role };
        var service = new Mock<IUserManagementService>(MockBehavior.Strict);
        var handler = new UpdateUserRoleCommandHandler(service.Object, user);

        // Act
        var result = await handler.Handle(new UpdateUserRoleCommand(self ? user.UserId : WorkflowFixture.OtherId, requested), default);

        // Assert
        Assert.Equal(expected, result.ErrorCode);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RoleChange_AdminForwardsServiceFailure()
    {
        // Arrange
        var user = new TestUserContext { Role = RoleNames.Admin };
        var service = new Mock<IUserManagementService>(MockBehavior.Strict);
        service.Setup(s => s.UpdateUserRoleAsync(WorkflowFixture.OtherId, UserRole.Organizer, default))
            .ReturnsAsync(RequestResult<bool>.Failure(ErrorCode.RoleNotFound));

        // Act
        var result = await new UpdateUserRoleCommandHandler(service.Object, user).Handle(new UpdateUserRoleCommand(WorkflowFixture.OtherId, UserRole.Organizer), default);

        // Assert
        Assert.Equal(ErrorCode.RoleNotFound, result.ErrorCode);
        service.VerifyAll();
    }
}
