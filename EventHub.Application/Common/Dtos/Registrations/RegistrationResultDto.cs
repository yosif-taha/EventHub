
namespace EventHub.Application.Common.Dtos.Registrations
{
    public record RegistrationResultDto(Guid RegistrationId, string? PaymentUrl = null);
}
