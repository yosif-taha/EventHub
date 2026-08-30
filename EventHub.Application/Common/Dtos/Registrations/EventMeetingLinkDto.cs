namespace EventHub.Application.Common.Dtos.Registrations;

public sealed record EventMeetingLinkDto(
    Guid EventId,
    string EventTitle,
    DateTime EventDate,
    string OnlineMeetingUrl);
