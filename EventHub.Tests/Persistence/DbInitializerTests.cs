using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Persistence.DataSeeding;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EventHub.Tests.Persistence;

[Trait("Category", "Persistence")]
public class DbInitializerTests
{
    [Theory]
    [InlineData("Development", true, 0)]
    [InlineData("Production", false, 0)]
    [InlineData("Production", true, 1)]
    public async Task Initialization_CreatesRolesAndBootstrapsOnlyWhenEnabledOutsideDevelopment(string environment, bool enabled, int users)
    {
        // Arrange: the test context has no SQL Server migrations; migration execution is outside this SQLite test.
        using var fixture = new WorkflowFixture();
        var initializer = Create(fixture, environment, enabled);

        // Act
        await initializer.IntiliazeAsync();
        fixture.Db.ChangeTracker.Clear();
        await initializer.IntiliazeAsync();

        // Assert
        Assert.Equal(new[] { "Admin", "Attendee", "Organizer" }, await fixture.Db.Roles.OrderBy(r => r.Name).Select(r => r.Name).ToArrayAsync());
        Assert.Equal(users, await fixture.Db.Users.CountAsync());
        if (users == 1)
        {
            var admin = await fixture.Db.Users.SingleAsync();
            Assert.True(admin.EmailConfirmed);
            Assert.Equal(UserRole.Admin, admin.Role);
            Assert.True(await fixture.Service<UserManager<ApplicationUser>>().IsInRoleAsync(admin, RoleNames.Admin));
            Assert.True(await fixture.Service<UserManager<ApplicationUser>>().CheckPasswordAsync(admin, "Bootstrap!Pass123"));
        }
    }

    [Fact]
    public async Task Initialization_DoesNotPromoteExistingEmailToAdministrator()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        await manager.CreateAsync(new ApplicationUser { UserName = "bootstrap@example.test", Email = "bootstrap@example.test", FullName = "Existing" });
        fixture.Db.ChangeTracker.Clear();

        // Act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Create(fixture, "Production", true).IntiliazeAsync());

        // Assert
        Assert.Contains("already belongs to a user", error.Message);
        Assert.Empty(await manager.GetUsersInRoleAsync(RoleNames.Admin));
        Assert.Equal(1, await fixture.Db.Users.CountAsync());
    }

    [Fact]
    public async Task Initialization_MigratesLegacyAttendeeRoleAndSynchronizesPrimaryRole()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        var roles = fixture.Service<RoleManager<IdentityRole<Guid>>>();
        await roles.CreateAsync(new IdentityRole<Guid>("Attend"));
        var user = new ApplicationUser { UserName = "legacy", FullName = "Legacy", Role = UserRole.Attendee };
        await manager.CreateAsync(user);
        await manager.AddToRoleAsync(user, "Attend");
        fixture.Db.ChangeTracker.Clear();

        // Act
        await Create(fixture, "Production", false).IntiliazeAsync();

        // Assert
        var stored = (await manager.FindByIdAsync(user.Id.ToString()))!;
        Assert.Equal(new[] { RoleNames.Attendee }, await manager.GetRolesAsync(stored));
        Assert.False(await roles.RoleExistsAsync("Attend"));
        Assert.Equal(UserRole.Attendee, stored.Role);
    }

    [Fact]
    public async Task Initialization_AssignsAttendeeToUserWithNoRecognizedRole()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "norole", FullName = "No Role", Role = UserRole.Admin };
        await manager.CreateAsync(user);
        fixture.Db.ChangeTracker.Clear();

        // Act
        await Create(fixture, "Production", false).IntiliazeAsync();

        // Assert
        var stored = await fixture.Db.Users.SingleAsync();
        Assert.Equal(UserRole.Attendee, stored.Role);
        Assert.Equal(new[] { RoleNames.Attendee }, await manager.GetRolesAsync(stored));
    }

    private static DbInitializer Create(WorkflowFixture fixture, string environment, bool enabled) =>
        new(fixture.Db, fixture.Service<UserManager<ApplicationUser>>(), fixture.Service<RoleManager<IdentityRole<Guid>>>(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["ASPNETCORE_ENVIRONMENT"] = environment,
                ["BootstrapAdminSettings:Enabled"] = enabled.ToString(),
                ["BootstrapAdminSettings:Email"] = "bootstrap@example.test",
                ["BootstrapAdminSettings:Password"] = "Bootstrap!Pass123",
                ["BootstrapAdminSettings:FullName"] = "Bootstrap Administrator"
            }).Build());
}
