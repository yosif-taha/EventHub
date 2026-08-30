namespace EventHub.MVC.Models.Organizers;

public sealed class OrganizerDashboardDto
{
    public int TotalEvents { get; init; }
    public int TotalRegistrations { get; init; }
    public int TotalAttendees { get; init; }
    public IReadOnlyList<EventStatusCountDto> EventStatusOverview { get; init; } = [];
}

public sealed record EventStatusCountDto(string Status, int Count);
