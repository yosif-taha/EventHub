using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Events.Create_Event;
using EventHub.Application.Features.Events.Update_Event;
using EventHub.Application.Features.Events.Update_Event_Status;
using EventHub.Application.Features.Events.Delete_Event;
using EventHub.Domin.Enums;
using EventHub.Tests.Support;
using MediatR;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EventHub.Tests.WebAPI;

[Trait("Category", "Http")]
public class EventMutationHttpTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_MapsCompleteRequestAndReturnsIdOrValidationFailure(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Organizer");
        var command = new CreateEventCommand("Conference", "Description", WorkflowFixture.Future, 25, "Cairo", WorkflowFixture.OtherId, 20, EventMode.Online, "https://meet.example.test/room");
        fixture.Mediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<Guid>.Success(WorkflowFixture.OwnerId) : RequestResult<Guid>.Failure(ErrorCode.ValidationError, "Invalid event"));

        // Act
        var response = await fixture.Client.PostAsJsonAsync("/api/events", command);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (success) Assert.Equal(WorkflowFixture.OwnerId, json.GetProperty("data").GetGuid());
        else Assert.Equal("Invalid event", json.GetProperty("message").GetString());
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_RouteIdOverridesConflictingBodyId(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Organizer");
        var command = new UpdateEventCommand(WorkflowFixture.OtherId, "Updated title", null, null, null, null, 20, null, null);
        fixture.Mediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<Unit>.Success(Unit.Value) : RequestResult<Unit>.Failure(ErrorCode.EventCapacityFull));

        // Act
        var response = await fixture.Client.PutAsJsonAsync("/api/events/" + WorkflowFixture.OtherId,
            new { id = WorkflowFixture.AttendeeId, title = "Updated title", maxAttendees = 20 });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.EventCapacityFull, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StatusChange_BindsRequestedTransitionAndReturnsOutcome(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Admin");
        fixture.Mediator.Setup(m => m.Send(new UpdateEventStatusCommand(WorkflowFixture.OtherId, EventStatus.Canceled), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<Unit>.Success(Unit.Value) : RequestResult<Unit>.Failure(ErrorCode.EventInvalidStatusTransition));

        // Act
        var response = await fixture.Client.PatchAsJsonAsync("/api/events/" + WorkflowFixture.OtherId + "/status", new { status = EventStatus.Canceled });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ReturnsSuccessOrMissingEventEnvelope(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Admin");
        fixture.Mediator.Setup(m => m.Send(new DeleteEventCommand(WorkflowFixture.OtherId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<Unit>.Success(Unit.Value) : RequestResult<Unit>.Failure(ErrorCode.EventNotFound));

        // Act
        var response = await fixture.Client.DeleteAsync("/api/events/" + WorkflowFixture.OtherId);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        fixture.Mediator.VerifyAll();
    }
}
