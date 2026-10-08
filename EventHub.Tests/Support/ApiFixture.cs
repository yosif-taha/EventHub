using EventHub.Infrastructure.Payment;
using EventHub.WebAPI.Presentation.Controllers;
using EventHub.WebAPI.Presentation.Extensions;
using EventHub.WebAPI.Presentation.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MediatR;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace EventHub.Tests.Support;

// Exercises production routing, model binding, JWT authorization, mapping, and error middleware.
// Application workflows are covered independently with real MediatR and SQLite.
internal sealed class ApiFixture : IDisposable
{
    public const string SigningKey = "test-only-signing-key-at-least-32-bytes-long";
    public const string HmacSecret = "test-webhook-secret";
    public Mock<IMediator> Mediator { get; } = new(MockBehavior.Strict);
    private readonly TestServer server;
    public HttpClient Client { get; }

    public ApiFixture(string? hmacSecret = HmacSecret)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Jwt:Key"] = SigningKey, ["Jwt:Issuer"] = "tests", ["Jwt:Audience"] = "tests", ["Jwt:ExpiresMinutes"] = "30"
        }).Build();
        server = new TestServer(new WebHostBuilder().ConfigureServices(services => {
            services.AddLogging();
            services.AddPresentation(configuration);
            services.RemoveAll<IHostedService>();
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            services.AddControllers().AddApplicationPart(typeof(EventController).Assembly);
            services.AddSingleton(Mediator.Object);
            services.Configure<PaymobSettings>(s => s.HmacSecret = hmacSecret ?? "");
        }).Configure(app => {
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }));
        Client = server.CreateClient();
    }

    public void Authenticate(string role, bool expired = false)
    {
        var token = new JwtSecurityToken("tests", "tests",
            [new Claim(ClaimTypes.NameIdentifier, WorkflowFixture.AttendeeId.ToString()), new Claim(ClaimTypes.Role, role)],
            expires: expired ? new DateTime(2000, 1, 1) : WorkflowFixture.Future,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    public void Dispose() { Client.Dispose(); server.Dispose(); }
}
