using EventHub.Domin.Enums;
using EventHub.MVC.Models.Common;

namespace EventHub.MVC.Models.Admins;

public sealed class AdminRegistrationsViewModel
{
    public PaginatedResult<AdminRegistrationDto> Registrations { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

public sealed class AdminRegistrationDto
{
    public Guid RegistrationId { get; init; }
    public Guid AttendeeId { get; init; }
    public string AttendeeName { get; init; } = string.Empty;
    public string AttendeeEmail { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public RegistrationStatus RegistrationStatus { get; init; }
    public DateTime RegistrationDate { get; init; }
    public PaymentTransactionStatus? PaymentStatus { get; init; }
    public decimal? PaymentAmount { get; init; }
    public string? PaymentCurrency { get; init; }
}
