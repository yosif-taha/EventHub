namespace EventHub.MVC.Models.Organizers;

public sealed class OrganizerDashboardViewModel
{
    public OrganizerDashboardDto Dashboard { get; init; } = new();
    public string? ErrorMessage { get; init; }
}
