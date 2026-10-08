using EventHub.Application.Common.Responses;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Infrastructure.Auth;
using EventHub.Infrastructure.Account;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace EventHub.Tests.Infrastructure;

[Trait("Category", "Integration")]
public class IdentityWorkflowTests
{
    [Fact]
    public async Task RegisterConfirmLoginAndRefresh_PersistIdentityAndRotateRefreshToken()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var users = fixture.Service<UserManager<ApplicationUser>>();
        var roles = fixture.Service<RoleManager<IdentityRole<Guid>>>();
        Assert.True((await roles.CreateAsync(new IdentityRole<Guid>(RoleNames.Attendee))).Succeeded);
        var service = Auth(fixture);
        string? confirmationLink = null;
        fixture.Email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), "Confirm your email", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => confirmationLink = body)
            .Returns(Task.CompletedTask);

        // Act
        var registration = await service.RegisterAsync("new@example.test", "Strong!Pass123", "New Attendee", "+201000000000", default);
        fixture.Db.ChangeTracker.Clear(); // Separate HTTP requests use separate tracking scopes.
        var unconfirmed = await service.LoginAsync("new@example.test", "Strong!Pass123", default);
        var code = QueryHelpers.ParseQuery(new Uri(confirmationLink!).Query)["code"].ToString();
        var confirmation = await service.ConfirmEmailAsync(registration.Data.ToString(), code, default);
        fixture.Db.ChangeTracker.Clear();
        var login = await service.LoginAsync("new@example.test", "Strong!Pass123", default);
        Assert.True(login.IsSuccess, login.ErrorCode.ToString());
        fixture.Db.ChangeTracker.Clear();
        Assert.NotEmpty((await fixture.Db.Users.Include(u => u.RefreshTokens).SingleAsync()).RefreshTokens);
        var refresh = await service.GenerateNewTokensAsync(login.Data!.Token, login.Data.RefreshToken, default);
        fixture.Db.ChangeTracker.Clear();
        var replay = await service.GenerateNewTokensAsync(login.Data.Token, login.Data.RefreshToken, default);

        // Assert
        Assert.True(registration.IsSuccess);
        Assert.Equal(ErrorCode.EmailNotConfirmed, unconfirmed.ErrorCode);
        Assert.True(confirmation.IsSuccess);
        Assert.True(login.IsSuccess);
        Assert.True(refresh.IsSuccess, refresh.ErrorCode + ": " + refresh.Message);
        Assert.Equal(ErrorCode.InvalidRefreshToken, replay.ErrorCode);
        Assert.NotEqual(login.Data.RefreshToken, refresh.Data!.RefreshToken);
        Assert.Equal(64, Convert.FromBase64String(refresh.Data.RefreshToken).Length);
        var user = await users.FindByEmailAsync("new@example.test");
        Assert.True(await users.IsInRoleAsync(user!, RoleNames.Attendee));
        Assert.Equal(2, user!.RefreshTokens.Count);
        Assert.Single(user.RefreshTokens, t => t.IsActive);
    }

    [Theory]
    [InlineData("missing", ErrorCode.UserNotFound)]
    [InlineData("password", ErrorCode.InvalidCredentials)]
    [InlineData("unconfirmed", ErrorCode.EmailNotConfirmed)]
    public async Task Login_RejectsInvalidCredentialsOrUnconfirmedAccount(string scenario, ErrorCode expected)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        if (scenario != "missing")
            Assert.True((await manager.CreateAsync(new ApplicationUser { Email = "user@example.test", UserName = "user@example.test", FullName = "User" }, "Strong!Pass123")).Succeeded);

        // Act
        var result = await Auth(fixture).LoginAsync("user@example.test", scenario == "password" ? "wrong" : "Strong!Pass123", default);

        // Assert
        Assert.Equal(expected, result.ErrorCode);
        Assert.All(await fixture.Db.Users.ToListAsync(), u => Assert.Empty(u.RefreshTokens));
    }

    [Fact]
    public async Task PasswordReset_UsesEncodedPublicLinkAndChangesAcceptedPassword()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Email = "user+tag@example.test", UserName = "user+tag@example.test", FullName = "User", EmailConfirmed = true };
        Assert.True((await manager.CreateAsync(user, "Strong!Pass123")).Succeeded);
        fixture.Db.ChangeTracker.Clear();
        var service = Auth(fixture);
        string? link = null;
        fixture.Email.Setup(e => e.SendEmailAsync(user.Email, "Reset Password", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => link = body).Returns(Task.CompletedTask);

        // Act
        var sent = await service.SendResetPasswordAsync(user.Email, default);
        var query = QueryHelpers.ParseQuery(new Uri(link!).Query);
        var reset = await service.ResetPasswordAsync(user.Email, query["code"].ToString(), "Changed!Pass456", default);

        // Assert
        Assert.True(sent.IsSuccess);
        Assert.Equal("/Auth/ResetPassword", new Uri(link!).AbsolutePath);
        Assert.Equal(user.Email, query["email"]);
        Assert.True(reset.IsSuccess);
        fixture.Db.ChangeTracker.Clear();
        var updatedUser = (await manager.FindByIdAsync(user.Id.ToString()))!;
        Assert.True(await manager.CheckPasswordAsync(updatedUser, "Changed!Pass456"));
        Assert.False(await manager.CheckPasswordAsync(updatedUser, "Strong!Pass123"));
    }

    [Fact]
    public async Task InvalidEncodedCodes_ReturnFailureWithoutChangingIdentity()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Email = "user@example.test", UserName = "user@example.test", FullName = "User" };
        await manager.CreateAsync(user, "Strong!Pass123");
        var service = Auth(fixture);

        // Act
        var confirmation = await service.ConfirmEmailAsync(user.Id.ToString(), "!", default);
        user.EmailConfirmed = true; await manager.UpdateAsync(user);
        var reset = await service.ResetPasswordAsync(user.Email, "!", "Changed!Pass456", default);

        // Assert
        Assert.Equal(ErrorCode.InvalidCredentials, confirmation.ErrorCode);
        Assert.Equal(ErrorCode.InvalidCredentials, reset.ErrorCode);
        Assert.True(await manager.CheckPasswordAsync(user, "Strong!Pass123"));
    }

    [Fact]
    public async Task RoleChange_ProtectsLastAdministratorAndSynchronizesIdentityRoles()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        var roles = fixture.Service<RoleManager<IdentityRole<Guid>>>();
        foreach (var name in new[] { RoleNames.Admin, RoleNames.Organizer, RoleNames.Attendee })
            await roles.CreateAsync(new IdentityRole<Guid>(name));
        var admin = new ApplicationUser { UserName = "admin", FullName = "Admin", Role = UserRole.Admin };
        var other = new ApplicationUser { UserName = "other", FullName = "Other", Role = UserRole.Attendee };
        await manager.CreateAsync(admin); await manager.AddToRoleAsync(admin, RoleNames.Admin);
        await manager.CreateAsync(other); await manager.AddToRoleAsync(other, RoleNames.Attendee);
        var service = new UserManagementService(manager, roles, fixture.UnitOfWork);

        // Act
        var blocked = await service.UpdateUserRoleAsync(admin.Id, UserRole.Organizer, default);
        var promoted = await service.UpdateUserRoleAsync(other.Id, UserRole.Admin, default);
        var demoted = await service.UpdateUserRoleAsync(admin.Id, UserRole.Organizer, default);

        // Assert
        Assert.Equal(ErrorCode.ValidationError, blocked.ErrorCode);
        Assert.True(promoted.IsSuccess);
        Assert.True(demoted.IsSuccess);
        Assert.Equal(new[] { RoleNames.Organizer }, await manager.GetRolesAsync((await manager.FindByIdAsync(admin.Id.ToString()))!));
        Assert.Equal(UserRole.Organizer, (await fixture.Db.Users.SingleAsync(u => u.Id == admin.Id)).Role);
    }

    [Theory]
    [InlineData(null, "+201000000000")]
    [InlineData("+201111111111", "+201111111111")]
    public async Task ProfileUpdate_PreservesPhoneWhenOmitted(string? phone, string expectedPhone)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "user", Email = "user@example.test", FullName = "Old Name", PhoneNumber = "+201000000000" };
        await manager.CreateAsync(user);
        var service = new AccountService(manager);

        // Act
        // SQLite stores GUID text in uppercase; SQL Server uses its own conversion.
        var sqliteUserId = user.Id.ToString().ToUpperInvariant();
        var update = await service.UpdateUserProfileAsync(sqliteUserId, "New Name", phone);
        var profile = await service.GetUserProfileAsync(sqliteUserId, default);

        // Assert
        Assert.True(update.IsSuccess);
        Assert.Equal("New Name", profile.Data!.FullName);
        Assert.Equal(expectedPhone, profile.Data.PhoneNumber);
        Assert.Equal(user.Email, profile.Data.Email);
    }

    private static AuthService Auth(WorkflowFixture fixture) =>
        new(fixture.Service<UserManager<ApplicationUser>>(),
            new JwtProvider(Options.Create(new JwtOptions { Key = "test-only-signing-key-at-least-32-bytes-long", Issuer = "tests", Audience = "tests", ExpiresMinutes = 30 }), fixture.Service<UserManager<ApplicationUser>>()),
            fixture.Email.Object, Options.Create(new AuthSettings { PublicBaseUrl = "https://app.example.test/" }));
}
