using EventHub.MVC.Models.Common;

namespace EventHub.MVC.Models.Events;

public sealed class EventListViewModel
{
    public EventListFilter Filter { get; init; } = new();
    public PaginatedResult<EventSummaryDto> Events { get; init; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
