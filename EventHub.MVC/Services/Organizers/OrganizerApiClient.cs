using EventHub.Domin.Enums;
using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Organizers;
using EventHub.MVC.Services.Api;

namespace EventHub.MVC.Services.Organizers;

public sealed class OrganizerApiClient(IBackendApiClient backendApiClient) : IOrganizerApiClient
{
    public Task<ApiCallResult<OrganizerDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<OrganizerDashboardDto>(BackendApiClientNames.Authenticated, "api/organizer/dashboard", cancellationToken);

    public Task<ApiCallResult<PaginatedResult<EventSummaryDto>>> GetEventsAsync(EventListFilter filter, CancellationToken cancellationToken)
    {
        var parameters = new List<string>
        {
            $"pageNumber={filter.PageNumber}",
            $"pageSize={filter.PageSize}",
            $"sortDirection={Uri.EscapeDataString(filter.SortDirection)}"
        };

        if (!string.IsNullOrWhiteSpace(filter.SearchValue))
            parameters.Add($"searchValue={Uri.EscapeDataString(filter.SearchValue)}");
        if (filter.CategoryId.HasValue)
            parameters.Add($"categoryId={filter.CategoryId.Value}");
        if (!string.IsNullOrWhiteSpace(filter.SortColumn))
            parameters.Add($"sortColumn={Uri.EscapeDataString(filter.SortColumn)}");

        return backendApiClient.GetAsync<PaginatedResult<EventSummaryDto>>(
            BackendApiClientNames.Authenticated,
            $"api/organizer/events?{string.Join('&', parameters)}",
            cancellationToken);
    }

    public Task<ApiCallResult<EventDetailsDto>> GetEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<EventDetailsDto>(BackendApiClientNames.Authenticated, $"api/events/{eventId}/management", cancellationToken);

    public Task<ApiCallResult<Guid>> CreateEventAsync(CreateOrganizerEventRequest request, CancellationToken cancellationToken) =>
        backendApiClient.PostAsync<CreateOrganizerEventRequest, Guid>(BackendApiClientNames.Authenticated, "api/events", request, cancellationToken);

    public Task<ApiCallResult<object>> UpdateEventAsync(Guid eventId, UpdateOrganizerEventRequest request, CancellationToken cancellationToken) =>
        backendApiClient.PutAsync<UpdateOrganizerEventRequest, object>(BackendApiClientNames.Authenticated, $"api/events/{eventId}", request, cancellationToken);

    public Task<ApiCallResult<object>> UpdateEventStatusAsync(Guid eventId, EventStatus status, CancellationToken cancellationToken) =>
        backendApiClient.PatchAsync<UpdateEventStatusRequest, object>(
            BackendApiClientNames.Authenticated,
            $"api/events/{eventId}/status",
            new UpdateEventStatusRequest(status),
            cancellationToken);

    public Task<ApiCallResult<PaginatedResult<EventRegistrationDto>>> GetAttendeesAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<PaginatedResult<EventRegistrationDto>>(
            BackendApiClientNames.Authenticated,
            $"api/events/{eventId}/registrations?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken);

    public Task<ApiCallResult<object>> SendAnnouncementAsync(SendEventAnnouncementRequest request, CancellationToken cancellationToken) =>
        backendApiClient.PostAsync<SendEventAnnouncementRequest, object>(BackendApiClientNames.Authenticated, "api/notifications", request, cancellationToken);
}
