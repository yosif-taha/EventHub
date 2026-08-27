namespace EventHub.Application.Common.Dtos.Dashboards
{
    public class OrganizerDashboardDto
    {
        public int TotalEvents { get; init; }
        public int TotalRegistrations { get; init; }
        public int TotalAttendees { get; init; }
        public IReadOnlyList<EventStatusCountDto> EventStatusOverview { get; init; } = [];
    }
}
