namespace EventHub.MVC.Models.Events;

using EventHub.MVC.Models.Registrations;

public sealed class EventDetailsViewModel
{
    public EventDetailsDto Event { get; init; } = new();
    public EventAvailabilityDto Availability { get; init; } = new();
    public EventMeetingLinkDto? AttendeeMeetingLink { get; init; }
}
