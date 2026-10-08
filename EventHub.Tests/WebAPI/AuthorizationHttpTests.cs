using EventHub.Tests.Support;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EventHub.Tests.WebAPI;

[Trait("Category", "Http")]
public class AuthorizationHttpTests
{
    [Theory]
    [InlineData("GET", "/api/admin/dashboard", "Organizer")]
    [InlineData("GET", "/api/admin/registrations", "Attendee")]
    [InlineData("GET", "/api/Admin/GetUsers", "Organizer")]
    [InlineData("GET", "/api/organizer/dashboard", "Admin")]
    [InlineData("GET", "/api/organizer/events", "Attendee")]
    [InlineData("GET", "/api/registrations/me", "Organizer")]
    [InlineData("POST", "/api/registrations", "Admin")]
    [InlineData("POST", "/api/notifications", "Attendee")]
    [InlineData("POST", "/api/Category/CreateCategory", "Organizer")]
    [InlineData("POST", "/api/Category/UpdateCategory", "Attendee")]
    [InlineData("DELETE", "/api/events/33333333-3333-3333-3333-333333333333", "Organizer")]
    public async Task RoleProtectedEndpoint_RejectsAnonymousAndWrongRoleBeforeCallingApplication(string method, string route, string wrongRole)
    {
        // Arrange
        using var fixture = new ApiFixture();
        using var anonymousRequest = new HttpRequestMessage(new HttpMethod(method), route) { Content = JsonContent.Create(new { }) };

        // Act
        var anonymous = await fixture.Client.SendAsync(anonymousRequest);
        fixture.Authenticate(wrongRole);
        using var roleRequest = new HttpRequestMessage(new HttpMethod(method), route) { Content = JsonContent.Create(new { }) };
        var forbidden = await fixture.Client.SendAsync(roleRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        fixture.Mediator.VerifyNoOtherCalls();
    }
}
