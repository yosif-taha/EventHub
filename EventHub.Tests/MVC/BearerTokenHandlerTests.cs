using EventHub.MVC.Services;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net.Http.Headers;
using System.Security.Claims;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class BearerTokenHandlerTests
{
    [Theory]
    [InlineData(null, null, null)]
    [InlineData("cookie-token", null, "Bearer cookie-token")]
    [InlineData("cookie-token", "explicit-token", "Bearer explicit-token")]
    [InlineData("", null, null)]
    public async Task Handler_UsesCookieTokenOnlyWhenAuthorizationIsAbsent(string? cookieToken, string? existingToken, string? expected)
    {
        // Arrange
        var authentication = new Mock<IAuthenticationService>();
        var properties = new AuthenticationProperties();
        if (cookieToken != null) properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = cookieToken }]);
        authentication.Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("cookie")), properties, CookieAuthenticationDefaults.AuthenticationScheme)));
        using var services = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        using var transport = new RecordingHttpHandler(_ => RecordingHttpHandler.Json("{}"));
        using var handler = new BackendBearerTokenHandler(new HttpContextAccessor { HttpContext = context }) { InnerHandler = transport };
        using var client = new HttpClient(handler);
        if (existingToken != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", existingToken);

        // Act
        await client.GetAsync("https://backend.example.test/events");

        // Assert
        Assert.Equal(expected, Assert.Single(transport.Requests).Authorization);
        authentication.Verify(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme), Times.Exactly(existingToken == null ? 1 : 0));
    }
}
