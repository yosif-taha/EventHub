using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Services.Api;

namespace EventHub.MVC.Services.Events;

public sealed class EventApiClient(IBackendApiClient backendApiClient) : IEventApiClient
{
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
            BackendApiClientNames.Public,
            $"api/events?{string.Join('&', parameters)}",
            cancellationToken);
    }

    public Task<ApiCallResult<EventDetailsDto>> GetEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<EventDetailsDto>(BackendApiClientNames.Public, $"api/events/{eventId}", cancellationToken);

    public Task<ApiCallResult<EventAvailabilityDto>> GetAvailabilityAsync(Guid eventId, CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<EventAvailabilityDto>(BackendApiClientNames.Public, $"api/Event/CheckEventAvailability?id={eventId}", cancellationToken);

    public Task<ApiCallResult<List<CategoryDto>>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        backendApiClient.GetAsync<List<CategoryDto>>(BackendApiClientNames.Public, "api/Category/GetAllCategories", cancellationToken);
}
