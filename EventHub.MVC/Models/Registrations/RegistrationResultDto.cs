namespace EventHub.MVC.Models.Registrations;

public sealed class RegistrationResultDto
{
    public Guid RegistrationId { get; init; }
    public string? PaymentUrl { get; init; }
}
