using EventHub.Domin.Enums;
using EventHub.MVC.Models.Admins;
using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Organizers;
using EventHub.MVC.Services.Api;

namespace EventHub.MVC.Services.Admins;

public sealed class AdminApiClient(IBackendApiClient backendApiClient) : IAdminApiClient
{
    public Task<ApiCallResult<AdminDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<AdminDashboardDto>(BackendApiClientNames.Authenticated, "api/admin/dashboard", cancellationToken);

    public Task<ApiCallResult<PaginatedResult<AdminUserDto>>> GetUsersAsync(AdminUserListFilter filter, CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<PaginatedResult<AdminUserDto>>(
            BackendApiClientNames.Authenticated,
            $"api/Admin/GetUsers?pageNumber={filter.PageNumber}&pageSize=25&searchValue={Uri.EscapeDataString(filter.SearchValue ?? string.Empty)}",
            cancellationToken);

    public Task<ApiCallResult<object>> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken) =>
        backendApiClient.PostAsync<UpdateUserRoleRequest, object>(
            BackendApiClientNames.Authenticated,
            $"api/Admin/UpdateUserRole/{userId}",
            new UpdateUserRoleRequest(role),
            cancellationToken);

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
            $"api/events?{string.Join('&', parameters)}",
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

    public Task<ApiCallResult<bool>> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        backendApiClient.DeleteAsync(BackendApiClientNames.Authenticated, $"api/events/{eventId}", cancellationToken);

    public Task<ApiCallResult<PaginatedResult<AdminEventRegistrationDto>>> GetEventRegistrationsAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<PaginatedResult<AdminEventRegistrationDto>>(
            BackendApiClientNames.Authenticated,
            $"api/events/{eventId}/registrations?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken);

    public Task<ApiCallResult<PaginatedResult<AdminRegistrationDto>>> GetRegistrationsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<PaginatedResult<AdminRegistrationDto>>(
            BackendApiClientNames.Authenticated,
            $"api/admin/registrations?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken);

    public Task<ApiCallResult<object>> SendAnnouncementAsync(SendEventAnnouncementRequest request, CancellationToken cancellationToken) =>
        backendApiClient.PostAsync<SendEventAnnouncementRequest, object>(BackendApiClientNames.Authenticated, "api/notifications", request, cancellationToken);
}
