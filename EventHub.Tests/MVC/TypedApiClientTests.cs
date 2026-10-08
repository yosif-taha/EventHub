using EventHub.Domin.Enums;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Admins;
using EventHub.MVC.Services;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Events;
using EventHub.MVC.Services.Admins;
using EventHub.MVC.Services.Organizers;
using EventHub.MVC.Services.Registrations;
using EventHub.Tests.Support;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EventHub.Tests.MVC;

[Trait("Category", "MockUnit")]
public class TypedApiClientTests
{
    [Theory]
    [InlineData("public", "/api/events", BackendApiClientNames.Public)]
    [InlineData("organizer", "/api/organizer/events", BackendApiClientNames.Authenticated)]
    [InlineData("admin", "/api/events", BackendApiClientNames.Authenticated)]
    public async Task EventSearch_EncodesFiltersAndUsesCorrectClient(string actor, string path, string clientName)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json("{\"isSuccess\":true,\"data\":{\"items\":[],\"totalCount\":0}}"));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };
        var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factory.Setup(f => f.CreateClient(clientName)).Returns(http);
        var backend = new BackendApiClient(factory.Object, NullLogger<BackendApiClient>.Instance);
        var filter = new EventListFilter { PageNumber = 2, PageSize = 25, SortColumn = "Title", SortDirection = "desc", SearchValue = "C# & music", CategoryId = WorkflowFixture.OtherId };

        // Act
        var result = actor switch {
            "public" => await new EventApiClient(backend).GetEventsAsync(filter, default),
            "organizer" => await new OrganizerApiClient(backend).GetEventsAsync(filter, default),
            _ => await new AdminApiClient(backend).GetEventsAsync(filter, default)
        };

        // Assert
        Assert.True(result.IsSuccess);
        var request = Assert.Single(handler.Requests);
        var uri = new Uri(request.Uri);
        Assert.Equal(path, uri.AbsolutePath);
        var query = QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal("C# & music", query["searchValue"]);
        Assert.Equal("2", query["pageNumber"]);
        Assert.Equal("25", query["pageSize"]);
        Assert.Equal("Title", query["sortColumn"]);
        Assert.Equal(WorkflowFixture.OtherId.ToString(), query["categoryId"]);
        factory.VerifyAll();
    }

    [Theory]
    [InlineData("register", "POST", "/api/registrations/33333333-3333-3333-3333-333333333333")]
    [InlineData("cancel", "DELETE", "/api/registrations/33333333-3333-3333-3333-333333333333")]
    [InlineData("status", "GET", "/api/registrations/33333333-3333-3333-3333-333333333333/status")]
    [InlineData("meeting", "GET", "/api/registrations/events/33333333-3333-3333-3333-333333333333/meeting-link")]
    [InlineData("mine", "GET", "/api/registrations/me")]
    public async Task RegistrationClient_UsesExpectedRouteAndVerb(string operation, string verb, string path)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => RecordingHttpHandler.Json("{\"isSuccess\":false,\"errorCode\":101,\"message\":\"Rejected\"}"));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://backend.example.test/") };
        var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factory.Setup(f => f.CreateClient(BackendApiClientNames.Authenticated)).Returns(http);
        var client = new RegistrationApiClient(new BackendApiClient(factory.Object, NullLogger<BackendApiClient>.Instance));

        // Act
        switch (operation)
        {
            case "register": Assert.False((await client.RegisterAsync(WorkflowFixture.OtherId, default)).IsSuccess); break;
            case "cancel": Assert.False((await client.CancelAsync(WorkflowFixture.OtherId, default)).IsSuccess); break;
            case "status": Assert.False((await client.GetStatusAsync(WorkflowFixture.OtherId, default)).IsSuccess); break;
            case "meeting": Assert.False((await client.GetMeetingLinkAsync(WorkflowFixture.OtherId, default)).IsSuccess); break;
            default: Assert.False((await client.GetMyAsync(2, 10, default)).IsSuccess); break;
        }

        // Assert
        var request = Assert.Single(handler.Requests);
        Assert.Equal(verb, request.Method.Method);
        Assert.Equal(path, new Uri(request.Uri).AbsolutePath);
        if (operation == "register") Assert.Null(request.Body);
        if (operation == "mine") Assert.Equal("2", QueryHelpers.ParseQuery(new Uri(request.Uri).Query)["pageNumber"]);
        factory.VerifyAll();
    }
}
