using EventHub.Domin.Enums;

namespace EventHub.MVC.Models.Registrations;

public sealed class UserRegistrationDto
{
    public Guid Id { get; init; }
    public DateTime RegistrationDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public DateTime EventStartDate { get; init; }
    public string EventLocation { get; init; } = string.Empty;
    public EventMode EventMode { get; init; }
    public bool PaymentRequired { get; init; }
    public PaymentTransactionStatus? PaymentStatus { get; init; }
}
