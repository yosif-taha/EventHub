using EventHub.Application.Common.Dtos.Admin;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Admin.GetUsers;
using EventHub.Tests.Support;
using Moq;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "MockUnit")]
public class GetUsersHandlerTests
{
    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Organizer", false)]
    [InlineData("Attendee", false)]
    public async Task UserList_IsAdminOnlyAndForwardsPaginationAndSearch(string role, bool allowed)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var user = new TestUserContext { Role = role };
        var service = new Mock<IUserManagementService>(MockBehavior.Strict);
        var page = new PaginatedList<UserRoleDto>([new(WorkflowFixture.OtherId, "user@example.test", "User", "Attendee")], 1, 2, 25);
        if (allowed) service.Setup(s => s.GetUsersAsync(2, 25, "User", cancellation.Token)).ReturnsAsync(RequestResult<PaginatedList<UserRoleDto>>.Success(page));
        var handler = new GetUsersQueryHandler(service.Object, user);

        // Act
        var result = await handler.Handle(new GetUsersQuery(2, 25, "User"), cancellation.Token);

        // Assert
        Assert.Equal(allowed, result.IsSuccess);
        if (allowed) { Assert.Same(page, result.Data); service.VerifyAll(); }
        else { Assert.Equal(ErrorCode.Forbidden, result.ErrorCode); service.VerifyNoOtherCalls(); }
    }
}
