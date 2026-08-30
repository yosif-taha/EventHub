namespace EventHub.MVC.Models.Registrations;

public sealed class EventMeetingLinkDto
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public string OnlineMeetingUrl { get; init; } = string.Empty;
}
