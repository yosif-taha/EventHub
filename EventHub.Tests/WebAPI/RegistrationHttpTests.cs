using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Registerations.RegisterationForEvent;
using EventHub.Application.Features.Registerations.CancelRegistrationForEvent;
using EventHub.Application.Features.Registerations.GetUserRegistrations;
using EventHub.Application.Features.Registerations.GetMyRegistrationStatus;
using EventHub.Application.Features.Registerations.GetMyEventMeetingLink;
using EventHub.Application.Features.Registerations.GetEventRegistrations;
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
public class RegistrationHttpTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task Register_BindsBothRoutesAndReturnsRegistrationOrBusinessError(bool bodyRoute, bool success)
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Authenticate("Attendee");
        fixture.Mediator.Setup(m => m.Send(new RegisterationCommand(WorkflowFixture.OtherId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<RegistrationResultDto>.Success(new(WorkflowFixture.AttendeeId, "https://provider.example.test/pay")) : RequestResult<RegistrationResultDto>.Failure(ErrorCode.EventIsFull));

        // Act
        var response = bodyRoute
            ? await fixture.Client.PostAsJsonAsync("/api/registrations", new { eventId = WorkflowFixture.OtherId })
            : await fixture.Client.PostAsync("/api/registrations/" + WorkflowFixture.OtherId, null);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (success) {
            Assert.Equal(WorkflowFixture.AttendeeId, json.GetProperty("data").GetProperty("registrationId").GetGuid());
            Assert.Equal("https://provider.example.test/pay", json.GetProperty("data").GetProperty("paymentUrl").GetString());
        }
        else Assert.Equal((int)ErrorCode.EventIsFull, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cancel_UsesRouteIdAndReturnsOutcome(bool success)
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Attendee");
        fixture.Mediator.Setup(m => m.Send(new CancelRegistrationCommand(WorkflowFixture.OtherId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? RequestResult<bool>.Success(true) : RequestResult<bool>.Failure(ErrorCode.RegistrationAlreadyCanceled));

        // Act
        var response = await fixture.Client.DeleteAsync("/api/registrations/" + WorkflowFixture.OtherId);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(success, json.GetProperty("isSuccess").GetBoolean());
        if (!success) Assert.Equal((int)ErrorCode.RegistrationAlreadyCanceled, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

    [Fact]
    public async Task MyRegistrations_PreservesPaginationAndMappedPaymentState()
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Attendee");
        fixture.Mediator.Setup(m => m.Send(new GetMyRegistrationsQuery(2, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<PaginatedList<UserRegistrationDto>>.Success(new([
                new() { Id = WorkflowFixture.AttendeeId, EventTitle = "Conference", Status = "Pending", PaymentRequired = true, PaymentStatus = PaymentTransactionStatus.Pending }
            ], 12, 2, 5)));

        // Act
        var json = await fixture.Client.GetFromJsonAsync<JsonElement>("/api/registrations/me?pageNumber=2&pageSize=5");

        // Assert
        Assert.True(json.GetProperty("isSuccess").GetBoolean());
        var data = json.GetProperty("data");
        Assert.Equal(12, data.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, data.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, data.GetProperty("pageNumber").GetInt32());
        var row = Assert.Single(data.GetProperty("items").EnumerateArray());
        Assert.Equal("Conference", row.GetProperty("eventTitle").GetString());
        Assert.True(row.GetProperty("paymentRequired").GetBoolean());
        fixture.Mediator.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Status_ReturnsWebhookAuthoritativeStateOrNotFoundEnvelope(bool found)
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Attendee");
        fixture.Mediator.Setup(m => m.Send(new GetMyRegistrationStatusQuery(WorkflowFixture.OtherId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(found ? RequestResult<RegistrationPaymentStatusDto>.Success(new() { RegistrationId = WorkflowFixture.OtherId, RegistrationStatus = RegistrationStatus.Pending, PaymentStatus = PaymentTransactionStatus.Pending, PaymentAmount = 100 })
                : RequestResult<RegistrationPaymentStatusDto>.Failure(ErrorCode.RegistrationNotFound));

        // Act
        var json = await fixture.Client.GetFromJsonAsync<JsonElement>("/api/registrations/" + WorkflowFixture.OtherId + "/status?success=true");

        // Assert
        Assert.Equal(found, json.GetProperty("isSuccess").GetBoolean());
        if (found) {
            Assert.Equal((int)RegistrationStatus.Pending, json.GetProperty("data").GetProperty("registrationStatus").GetInt32());
            Assert.Equal(100, json.GetProperty("data").GetProperty("paymentAmount").GetDecimal());
        }
        else Assert.Equal((int)ErrorCode.RegistrationNotFound, json.GetProperty("errorCode").GetInt32());
        fixture.Mediator.VerifyAll();
    }

    [Fact]
    public async Task MeetingLink_ReturnsPrivateUrlFromAuthorizedApplicationResult()
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Attendee");
        fixture.Mediator.Setup(m => m.Send(new GetMyEventMeetingLinkQuery(WorkflowFixture.OtherId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<EventMeetingLinkDto>.Success(new(WorkflowFixture.OtherId, "Conference", WorkflowFixture.Future, "https://private.example.test/room")));

        // Act
        var json = await fixture.Client.GetFromJsonAsync<JsonElement>("/api/registrations/events/" + WorkflowFixture.OtherId + "/meeting-link");

        // Assert
        Assert.Equal("https://private.example.test/room", json.GetProperty("data").GetProperty("onlineMeetingUrl").GetString());
        fixture.Mediator.VerifyAll();
    }

    [Fact]
    public async Task AttendeeList_MapsAttendeeIdentityAndStatus()
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Organizer");
        fixture.Mediator.Setup(m => m.Send(new GetEventRegistrationsQuery(WorkflowFixture.OtherId, 1, 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<PaginatedList<EventRegistrationDto>>.Success(new([
                new() { RegistrationId = WorkflowFixture.AttendeeId, AttendeeName = "Test Attendee", AttendeeEmail = "attendee@example.test", Status = "Confirmed" }
            ], 1, 1, 10)));

        // Act
        var json = await fixture.Client.GetFromJsonAsync<JsonElement>("/api/events/" + WorkflowFixture.OtherId + "/registrations");

        // Assert
        var row = Assert.Single(json.GetProperty("data").GetProperty("items").EnumerateArray());
        Assert.Equal("attendee@example.test", row.GetProperty("attendeeEmail").GetString());
        Assert.Equal("Confirmed", row.GetProperty("status").GetString());
        fixture.Mediator.VerifyAll();
    }
}
