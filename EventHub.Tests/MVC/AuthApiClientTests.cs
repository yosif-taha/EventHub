using EventHub.MVC.Models.Auth;
using EventHub.MVC.Services.Auth;
using EventHub.MVC.Services.Api;
using EventHub.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class AuthApiClientTests
{
    [Theory]
    [InlineData("login", "/api/Auth/Login")]
    [InlineData("register", "/api/Auth/Register")]
    [InlineData("confirm", "/api/Auth/ConfirmEmail")]
    [InlineData("reset", "/api/Auth/ResetPassword")]
    public async Task AuthRequests_UseCorrectContractsAndDoNotSendFormOnlyFields(string operation, string path)
    {
        // Arrange
        var response = operation switch {
            "login" => "{\"isSuccess\":true,\"data\":{\"id\":\"user\",\"token\":\"jwt\",\"expiresIn\":60}}",
            "register" => "{\"isSuccess\":true,\"data\":\"22222222-2222-2222-2222-222222222222\"}",
            _ => "{\"isSuccess\":true}"
        };
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json(response));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };
        var client = Client(http);

        // Act
        switch (operation)
        {
            case "login": Assert.True((await client.LoginAsync(new() { Email = "user@example.test", Password = "password", ReturnUrl = "/private" }, default)).IsSuccess); break;
            case "register": Assert.Equal(WorkflowFixture.AttendeeId, (await client.RegisterAsync(new() { Email = "user@example.test", Password = "password", ConfirmPassword = "password", FullName = "User" }, default)).Data); break;
            case "confirm": Assert.True((await client.ConfirmEmailAsync(WorkflowFixture.AttendeeId, "code+/", default)).Data); break;
            default: Assert.True((await client.ResetPasswordAsync(new() { Email = "user@example.test", Code = "code+/", NewPassword = "password", ConfirmPassword = "password" }, default)).Data); break;
        }

        // Assert
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(path, new Uri(request.Uri).AbsolutePath);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.False(body.RootElement.TryGetProperty("returnUrl", out _));
        Assert.False(body.RootElement.TryGetProperty("confirmPassword", out _));
        if (operation is "confirm" or "reset") Assert.Equal("code+/", body.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData(false, 401, null, ApiFailureKind.Unauthorized)]
    [InlineData(true, 403, null, ApiFailureKind.Forbidden)]
    [InlineData(false, 404, null, ApiFailureKind.NotFound)]
    [InlineData(true, 500, null, ApiFailureKind.Server)]
    [InlineData(false, 200, 101, ApiFailureKind.Validation)]
    [InlineData(true, 200, 104, ApiFailureKind.Unauthorized)]
    [InlineData(false, 200, 105, ApiFailureKind.Forbidden)]
    public async Task FailureResponse_PreservesBusinessAndHttpClassification(bool operation, int status, int? error, ApiFailureKind expected)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json(
            JsonSerializer.Serialize(new { isSuccess = false, errorCode = error, message = "Rejected" }), (HttpStatusCode)status));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };
        var client = Client(http);

        // Act
        var kind = operation
            ? (await client.ConfirmEmailAsync(WorkflowFixture.AttendeeId, "code", default)).FailureKind
            : (await client.LoginAsync(new(), default)).FailureKind;

        // Assert
        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MalformedResponse_IsSafeFailureForBothResponseShapes(bool operation)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json("<html>private exception</html>"));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };
        var client = Client(http);

        // Act
        var message = operation
            ? (await client.ConfirmEmailAsync(WorkflowFixture.AttendeeId, "code", default)).Message
            : (await client.LoginAsync(new(), default)).Message;

        // Assert
        Assert.DoesNotContain("private exception", message);
        Assert.Contains("try again", message);
    }

    private static AuthApiClient Client(HttpClient http)
    {
        var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factory.Setup(f => f.CreateClient(AuthApiClient.HttpClientName)).Returns(http);
        return new AuthApiClient(factory.Object, NullLogger<AuthApiClient>.Instance);
    }
}
