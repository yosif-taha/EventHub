namespace EventHub.MVC.Models.Admins;

public sealed class AdminDashboardDto
{
    public int TotalUsers { get; init; }
    public int TotalEvents { get; init; }
    public int TotalRegistrations { get; init; }
    public IReadOnlyList<StatusCountDto> EventStatusOverview { get; init; } = [];
    public PaymentDashboardDto Payments { get; init; } = new();
}

public sealed class PaymentDashboardDto
{
    public int TotalTransactions { get; init; }
    public int SuccessfulPayments { get; init; }
    public int PendingPayments { get; init; }
    public int FailedOrCanceledPayments { get; init; }
    public decimal SuccessfulPaymentAmount { get; init; }
    public IReadOnlyList<StatusCountDto> StatusOverview { get; init; } = [];
}

public sealed record StatusCountDto(string Status, int Count);

public sealed class AdminDashboardViewModel
{
    public AdminDashboardDto Dashboard { get; init; } = new();
    public string? ErrorMessage { get; init; }
}
