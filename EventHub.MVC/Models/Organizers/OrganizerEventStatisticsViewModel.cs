using EventHub.MVC.Models.Events;

namespace EventHub.MVC.Models.Organizers;

public sealed class OrganizerEventStatisticsViewModel
{
    public EventDetailsDto Event { get; init; } = new();
    public int RegistrationCount { get; init; }
    public string? ErrorMessage { get; init; }
}
