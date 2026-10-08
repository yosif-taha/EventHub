using EventHub.MVC.Controllers;
using EventHub.MVC.Extensions;
using EventHub.MVC.Services.Organizers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Security.Claims;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "Http")]
public class MvcSecurityHttpTests
{
    [Theory]
    [InlineData(null, "/Organizer/Events/Create", HttpStatusCode.Redirect)]
    [InlineData("Attendee", "/Organizer/Events/Create", HttpStatusCode.Redirect)]
    [InlineData("Organizer", "/Organizer/Events/Create", HttpStatusCode.BadRequest)]
    [InlineData("Admin", "/Admin/Events/Create", HttpStatusCode.BadRequest)]
    [InlineData("Attendee", "/Registrations/Create/33333333-3333-3333-3333-333333333333", HttpStatusCode.BadRequest)]
    [InlineData("Organizer", "/Auth/Logout", HttpStatusCode.BadRequest)]
    public async Task MutatingActions_EnforceCookieRoleAndAntiforgeryBeforeBusinessCalls(string? role, string route, HttpStatusCode expected)
    {
        // Arrange: the sign-in endpoint exists only in this in-process test host.
        var organizer = new Mock<IOrganizerApiClient>(MockBehavior.Strict);
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(new Dictionary<string, string?> { ["BackendApi:BaseUrl"] = "https://backend.example.test/" }))
            .ConfigureServices((context, services) => {
                services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                services.AddMvcPresentation(context.Configuration);
                services.AddControllersWithViews().ConfigureApplicationPartManager(parts => {
                    parts.ApplicationParts.Clear();
                    parts.ApplicationParts.Add(new AssemblyPart(typeof(OrganizerController).Assembly));
                });
                services.AddSingleton(organizer.Object);
            })
            .Configure(app => {
                app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
                app.UseEndpoints(endpoints => {
                    endpoints.MapGet("/test/signin", async context => {
                        var principal = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, "test-user"), new Claim(ClaimTypes.Role, context.Request.Query["role"].ToString())],
                            CookieAuthenticationDefaults.AuthenticationScheme));
                        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    });
                    endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                });
            }));
        using var client = server.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        if (role != null)
        {
            var signIn = await client.GetAsync("/test/signin?role=" + role);
            var cookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"));
            Assert.Contains("secure", cookie.ToLowerInvariant());
            Assert.Contains("httponly", cookie.ToLowerInvariant());
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Act
        var response = await client.PostAsync(route, new FormUrlEncodedContent([]));

        // Assert
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.Redirect)
            Assert.Contains(role == null ? "/Auth/Login" : "/Auth/AccessDenied", response.Headers.Location!.AbsolutePath);
        organizer.VerifyNoOtherCalls();
    }
}
