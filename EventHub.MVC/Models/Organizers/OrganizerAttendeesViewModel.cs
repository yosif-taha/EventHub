using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;

namespace EventHub.MVC.Models.Organizers;

public sealed class OrganizerAttendeesViewModel
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public PaginatedResult<EventRegistrationDto> Attendees { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

public sealed class EventRegistrationDto
{
    public Guid RegistrationId { get; init; }
    public Guid AttendeeId { get; init; }
    public string AttendeeName { get; init; } = string.Empty;
    public string AttendeeEmail { get; init; } = string.Empty;
    public DateTime RegistrationDate { get; init; }
    public string Status { get; init; } = string.Empty;
}
