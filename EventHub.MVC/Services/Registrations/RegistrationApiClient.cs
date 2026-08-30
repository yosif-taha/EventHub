using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Registrations;
using EventHub.MVC.Services.Api;

namespace EventHub.MVC.Services.Registrations;

public sealed class RegistrationApiClient(IBackendApiClient backendApiClient) : IRegistrationApiClient
{
    public Task<ApiCallResult<RegistrationResultDto>> RegisterAsync(Guid eventId, CancellationToken cancellationToken) =>
        backendApiClient.PostAsync<object, RegistrationResultDto>(
            BackendApiClientNames.Authenticated,
            $"api/registrations/{eventId}",
            null,
            cancellationToken);

    public Task<ApiCallResult<PaginatedResult<UserRegistrationDto>>> GetMyAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<PaginatedResult<UserRegistrationDto>>(
            BackendApiClientNames.Authenticated,
            $"api/registrations/me?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken);

    public Task<ApiCallResult<RegistrationPaymentStatusDto>> GetStatusAsync(Guid registrationId, CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<RegistrationPaymentStatusDto>(
            BackendApiClientNames.Authenticated,
            $"api/registrations/{registrationId}/status",
            cancellationToken);

    public Task<ApiCallResult<EventMeetingLinkDto>> GetMeetingLinkAsync(Guid eventId, CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<EventMeetingLinkDto>(
            BackendApiClientNames.Authenticated,
            $"api/registrations/events/{eventId}/meeting-link",
            cancellationToken);

    public Task<ApiCallResult<bool>> CancelAsync(Guid registrationId, CancellationToken cancellationToken) =>
        backendApiClient.DeleteAsync(BackendApiClientNames.Authenticated, $"api/registrations/{registrationId}", cancellationToken);
}
