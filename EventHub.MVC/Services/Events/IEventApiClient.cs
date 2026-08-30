using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Services.Api;

namespace EventHub.MVC.Services.Events;

public interface IEventApiClient
{
    Task<ApiCallResult<PaginatedResult<EventSummaryDto>>> GetEventsAsync(EventListFilter filter, CancellationToken cancellationToken);
    Task<ApiCallResult<EventDetailsDto>> GetEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<ApiCallResult<EventAvailabilityDto>> GetAvailabilityAsync(Guid eventId, CancellationToken cancellationToken);
    Task<ApiCallResult<List<CategoryDto>>> GetCategoriesAsync(CancellationToken cancellationToken);
}
