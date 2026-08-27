namespace EventHub.Application.Common.Dtos.Dashboards
{
    public class PaymentDashboardDto
    {
        public int TotalTransactions { get; init; }
        public int SuccessfulPayments { get; init; }
        public int PendingPayments { get; init; }
        public int FailedOrCanceledPayments { get; init; }
        public decimal SuccessfulPaymentAmount { get; init; }
        public IReadOnlyList<PaymentStatusCountDto> StatusOverview { get; init; } = [];
    }

    public record PaymentStatusCountDto(string Status, int Count);
}
