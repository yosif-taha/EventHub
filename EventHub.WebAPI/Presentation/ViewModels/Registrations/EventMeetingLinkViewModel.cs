namespace EventHub.WebAPI.Presentation.ViewModels.Registrations;

public sealed record EventMeetingLinkViewModel(
    Guid EventId,
    string EventTitle,
    DateTime EventDate,
    string OnlineMeetingUrl);
