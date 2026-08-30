using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;

namespace EventHub.MVC.Models.Organizers;

public sealed class OrganizerEventListViewModel
{
    public EventListFilter Filter { get; init; } = new();
    public PaginatedResult<EventSummaryDto> Events { get; init; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
