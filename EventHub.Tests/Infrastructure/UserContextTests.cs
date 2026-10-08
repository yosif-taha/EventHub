using EventHub.Infrastructure.Common;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace EventHub.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class UserContextTests
{
    [Fact]
    public void MissingContext_IsAnonymousWithNoIdentity()
    {
        // Arrange
        var user = new UserContext(new HttpContextAccessor());

        // Act / Assert
        Assert.Equal(Guid.Empty, user.UserId);
        Assert.Null(user.Email);
        Assert.False(user.IsAuthenticated);
        Assert.False(user.IsInRole("Admin"));
    }

    [Theory]
    [InlineData("22222222-2222-2222-2222-222222222222", true)]
    [InlineData("invalid", false)]
    public void Claims_ProvideIdentityAndRolesWithoutThrowingForInvalidId(string id, bool valid)
    {
        // Arrange
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Email, "user@example.test"), new Claim(ClaimTypes.Role, "Admin")], "test")) };
        var user = new UserContext(new HttpContextAccessor { HttpContext = context });

        // Act / Assert
        Assert.Equal(valid ? WorkflowFixture.AttendeeId : Guid.Empty, user.UserId);
        Assert.Equal("user@example.test", user.Email);
        Assert.True(user.IsAuthenticated);
        Assert.True(user.IsInRole("Admin"));
        Assert.False(user.IsInRole("Organizer"));
    }
}
