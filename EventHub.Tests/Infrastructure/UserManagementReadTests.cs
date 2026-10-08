using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Infrastructure.Account;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace EventHub.Tests.Infrastructure;

[Trait("Category", "Integration")]
public class UserManagementReadTests
{
    [Fact]
    public async Task UserSearch_TrimsSearchSortsByEmailAndPaginatesWithTotalCount()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var users = fixture.Service<UserManager<ApplicationUser>>();
        await users.CreateAsync(new ApplicationUser { UserName = "beta", Email = "b@example.test", FullName = "Match Two", Role = UserRole.Organizer });
        await users.CreateAsync(new ApplicationUser { UserName = "alpha", Email = "a@example.test", FullName = "Match One", Role = UserRole.Attendee });
        await users.CreateAsync(new ApplicationUser { UserName = "other", Email = "c@example.test", FullName = "Other" });
        fixture.Db.ChangeTracker.Clear();
        var service = new UserManagementService(users, fixture.Service<RoleManager<IdentityRole<Guid>>>(), fixture.UnitOfWork);

        // Act
        var page = await service.GetUsersAsync(2, 1, " Match ", default);
        var empty = await service.GetUsersAsync(1, 10, "not found", default);
        var count = await service.GetUserCountAsync(default);

        // Assert
        Assert.Equal(3, count);
        Assert.Equal(2, page.Data!.TotalCount);
        var row = Assert.Single(page.Data.Items);
        Assert.Equal("b@example.test", row.Email); Assert.Equal("Organizer", row.Role);
        Assert.True(page.Data.HasPreviousPage); Assert.False(page.Data.HasNextPage);
        Assert.Empty(empty.Data!.Items); Assert.Equal(0, empty.Data.TotalCount);
    }
}
