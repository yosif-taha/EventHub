namespace EventHub.Application.Common.Dtos.Dashboards
{
    public class AdminDashboardDto
    {
        public int TotalUsers { get; init; }
        public int TotalEvents { get; init; }
        public int TotalRegistrations { get; init; }
        public IReadOnlyList<EventStatusCountDto> EventStatusOverview { get; init; } = [];
        public PaymentDashboardDto Payments { get; init; } = new();
    }
}
