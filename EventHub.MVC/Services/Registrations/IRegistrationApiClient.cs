using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Registrations;
using EventHub.MVC.Services.Api;

namespace EventHub.MVC.Services.Registrations;

public interface IRegistrationApiClient
{
    Task<ApiCallResult<RegistrationResultDto>> RegisterAsync(Guid eventId, CancellationToken cancellationToken);
    Task<ApiCallResult<PaginatedResult<UserRegistrationDto>>> GetMyAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ApiCallResult<RegistrationPaymentStatusDto>> GetStatusAsync(Guid registrationId, CancellationToken cancellationToken);
    Task<ApiCallResult<EventMeetingLinkDto>> GetMeetingLinkAsync(Guid eventId, CancellationToken cancellationToken);
    Task<ApiCallResult<bool>> CancelAsync(Guid registrationId, CancellationToken cancellationToken);
}
