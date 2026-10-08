using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Events.Get_Event_By_Id;
using EventHub.Application.Features.Events.Create_Event;
using EventHub.Domin.Constants;
using EventHub.Tests.Support;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EventHub.Tests.WebAPI;

[Trait("Category", "Http")]
public class EventHttpTests
{
    [Theory]
    [InlineData("/api/events/22222222-2222-2222-2222-222222222222")]
    [InlineData("/api/Event/GetEventById?id=22222222-2222-2222-2222-222222222222")]
    public async Task PublicDetail_SupportsBothRoutesAndRemovesPrivateMeetingUrl(string route)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Mediator.Setup(m => m.Send(It.Is<GetEventByIdQuery>(q => q.Id == WorkflowFixture.AttendeeId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<EventDto>.Success(new EventDto { Id = WorkflowFixture.AttendeeId, Title = "Public title", OnlineMeetingUrl = "https://private.example.test/meeting" }));

        // Act
        var response = await fixture.Client.GetAsync(route);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(json.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("Public title", json.GetProperty("data").GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("data").GetProperty("onlineMeetingUrl").ValueKind);
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(null, false, HttpStatusCode.Unauthorized)]
    [InlineData(RoleNames.Attendee, false, HttpStatusCode.Forbidden)]
    [InlineData(RoleNames.Admin, true, HttpStatusCode.Unauthorized)]
    public async Task ProtectedWrite_RejectsUnauthenticatedWrongRoleAndExpiredTokens(string? role, bool expired, HttpStatusCode expected)
    {
        // Arrange
        using var fixture = new ApiFixture();
        if (role != null) fixture.Authenticate(role, expired);

        // Act
        var response = await fixture.Client.PostAsJsonAsync("/api/events", new { });

        // Assert
        Assert.Equal(expected, response.StatusCode);
        fixture.Mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MalformedJson_ReturnsBadRequestWithoutInvokingHandler()
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate(RoleNames.Admin);

        // Act
        var response = await fixture.Client.PostAsync("/api/events", new StringContent("{", Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        fixture.Mediator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ErrorCode.EventNotFound)]
    [InlineData(ErrorCode.ConcurrencyConflict)]
    public async Task BusinessFailure_IsReturnedInCurrentHttp200Envelope(ErrorCode error)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Mediator.Setup(m => m.Send(It.IsAny<GetEventByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<EventDto>.Failure(error));

        // Act
        var response = await fixture.Client.GetAsync("/api/events/" + WorkflowFixture.OtherId);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(json.GetProperty("isSuccess").GetBoolean());
        Assert.Equal((int)error, json.GetProperty("errorCode").GetInt32());
    }

    [Fact]
    public async Task UnhandledException_ReturnsGeneric500WithoutLeakingDetails()
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Mediator.Setup(m => m.Send(It.IsAny<GetEventByIdQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("private database details"));

        // Act
        var response = await fixture.Client.GetAsync("/api/events/" + WorkflowFixture.OtherId);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("internal server error", body);
        Assert.DoesNotContain("private database details", body);
    }
}
