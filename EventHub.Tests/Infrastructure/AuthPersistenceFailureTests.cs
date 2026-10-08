using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using EventHub.Infrastructure.Auth;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EventHub.Tests.Infrastructure;

[Trait("Category", "Integration")]
public class AuthPersistenceFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TokenMutation_RejectsFailedIdentitySaveInsteadOfReturningUnpersistedTokens(bool refresh)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var realManager = fixture.Service<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Email = "user@example.test", UserName = "user@example.test", FullName = "User", EmailConfirmed = true };
        if (refresh) user.RefreshTokens.Add(new() { Token = "existing", ExpiresOn = WorkflowFixture.Future });
        Assert.True((await realManager.CreateAsync(user)).Succeeded);
        fixture.Db.ChangeTracker.Clear();
        var manager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        manager.SetupGet(m => m.Users).Returns(fixture.Db.Users);
        manager.Setup(m => m.NormalizeEmail("user@example.test")).Returns("USER@EXAMPLE.TEST");
        manager.Setup(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), "password")).ReturnsAsync(true);
        manager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" }));
        var jwt = new Mock<IJwtProvider>();
        jwt.Setup(j => j.ValidateToken("jwt")).Returns(user.Id.ToString());
        jwt.Setup(j => j.GenerateTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(("new-jwt", 60));
        var service = new AuthService(manager.Object, jwt.Object, fixture.Email.Object, Options.Create(new AuthSettings { PublicBaseUrl = "https://app.example.test/" }));

        // Act
        var result = refresh
            ? await service.GenerateNewTokensAsync("jwt", "existing", default)
            : await service.LoginAsync("user@example.test", "password", default);

        // Assert
        Assert.Equal(ErrorCode.DatabaseError, result.ErrorCode);
        Assert.Null(result.Data);
        manager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        using var verification = fixture.CreateContext();
        var persisted = await verification.Users.SingleAsync();
        Assert.Equal(refresh ? 1 : 0, persisted.RefreshTokens.Count);
        if (refresh) Assert.Null(Assert.Single(persisted.RefreshTokens).RevokedOn);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    [InlineData("revoked")]
    public async Task Refresh_RejectsInactiveOrUnknownTokensWithoutIssuingNewCredentials(string state)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var manager = fixture.Service<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "user", Email = "user@example.test", FullName = "User", EmailConfirmed = true };
        user.RefreshTokens.Add(new() { Token = "existing", ExpiresOn = state == "expired" ? new DateTime(2000, 1, 1) : WorkflowFixture.Future,
            RevokedOn = state == "revoked" ? new DateTime(2026, 1, 1) : null });
        await manager.CreateAsync(user);
        fixture.Db.ChangeTracker.Clear();
        var jwt = new Mock<IJwtProvider>(MockBehavior.Strict);
        jwt.Setup(j => j.ValidateToken("jwt")).Returns(user.Id.ToString());
        var service = new AuthService(manager, jwt.Object, fixture.Email.Object, Options.Create(new AuthSettings()));

        // Act
        var result = await service.GenerateNewTokensAsync("jwt", state == "missing" ? "unknown" : "existing", default);

        // Assert
        Assert.Equal(ErrorCode.InvalidRefreshToken, result.ErrorCode);
        jwt.Verify(j => j.GenerateTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
        Assert.Single((await fixture.Db.Users.SingleAsync()).RefreshTokens);
    }
}
