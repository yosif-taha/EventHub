using EventHub.Domin.Enums;

namespace EventHub.Application.Common.Dtos.Registrations;

public sealed class RegistrationPaymentStatusDto
{
    public Guid RegistrationId { get; init; }
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public DateTime EventStartDate { get; init; }
    public string EventLocation { get; init; } = string.Empty;
    public EventMode EventMode { get; init; }
    public RegistrationStatus RegistrationStatus { get; init; }
    public bool PaymentRequired { get; init; }
    public PaymentTransactionStatus? PaymentStatus { get; init; }
    public decimal? PaymentAmount { get; init; }
    public string? PaymentCurrency { get; init; }
}
