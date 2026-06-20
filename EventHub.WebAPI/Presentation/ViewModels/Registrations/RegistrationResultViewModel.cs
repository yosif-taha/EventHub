namespace EventHub.WebAPI.Presentation.ViewModels.Registrations
{
    public record RegistrationResultViewModel(Guid RegistrationId, string? PaymentUrl = null);
}
