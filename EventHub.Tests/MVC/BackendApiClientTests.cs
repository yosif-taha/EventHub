using EventHub.MVC.Services.Api;
using EventHub.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class BackendApiClientTests
{
    [Theory]
    [InlineData(401, null, ApiFailureKind.Unauthorized)]
    [InlineData(403, null, ApiFailureKind.Forbidden)]
    [InlineData(404, null, ApiFailureKind.NotFound)]
    [InlineData(500, 101, ApiFailureKind.Server)]
    [InlineData(200, 101, ApiFailureKind.Validation)]
    [InlineData(200, 104, ApiFailureKind.Unauthorized)]
    [InlineData(200, 105, ApiFailureKind.Forbidden)]
    [InlineData(200, 300, ApiFailureKind.NotFound)]
    [InlineData(200, 400, ApiFailureKind.NotFound)]
    [InlineData(200, 999, ApiFailureKind.Server)]
    public async Task FailureEnvelope_MapsHttpAndBusinessErrors(int status, int? error, ApiFailureKind expected)
    {
        // Arrange
        var json = System.Text.Json.JsonSerializer.Serialize(new { isSuccess = false, message = "Failure details", errorCode = error });
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json(json, (HttpStatusCode)status));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };
        var backend = Backend(client);

        // Act
        var result = await backend.GetAsync<string>("test", "api/events", default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(expected, result.FailureKind);
        Assert.Equal(error, result.ErrorCode);
        Assert.Equal("Failure details", result.Message);
    }

    [Theory]
    [InlineData("post")]
    [InlineData("put")]
    [InlineData("patch")]
    public async Task SuccessfulWrite_SerializesBodyAndUnwrapsResponse(string verb)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json("{\"isSuccess\":true,\"data\":42}"));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };
        var backend = Backend(client);

        // Act
        var result = verb switch {
            "post" => await backend.PostAsync<object, int>("test", "api/events", new { Title = "Conference" }, default),
            "put" => await backend.PutAsync<object, int>("test", "api/events", new { Title = "Conference" }, default),
            _ => await backend.PatchAsync<object, int>("test", "api/events", new { Title = "Conference" }, default)
        };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(verb.ToUpperInvariant(), request.Method.Method);
        Assert.Equal("{\"title\":\"Conference\"}", request.Body);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task InvalidResponse_ReturnsSafeFailure(string body)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json(body));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };

        // Act
        var result = await Backend(client).GetAsync<string>("test", "api/events", default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiFailureKind.Server, result.FailureKind);
        Assert.DoesNotContain(body == "{}" ? "exception" : body, result.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportFailure_ReturnsUnavailable(bool timeout)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => throw (timeout ? new TaskCanceledException() : new HttpRequestException()));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };

        // Act
        var result = await Backend(client).GetAsync<string>("test", "api/events", default);

        // Assert
        Assert.Equal(ApiFailureKind.Unavailable, result.FailureKind);
    }

    [Fact]
    public async Task CallerCancellation_IsPropagated()
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json("{}"));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Backend(client).GetAsync<string>("test", "api/events", cancellation.Token));
    }

    private static BackendApiClient Backend(HttpClient client)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("test")).Returns(client);
        return new BackendApiClient(factory.Object, NullLogger<BackendApiClient>.Instance);
    }
}
