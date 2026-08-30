using EventHub.MVC.Models.Common;
using EventHub.MVC.Models.Events;

namespace EventHub.MVC.Models.Admins;

public sealed class AdminEventListViewModel
{
    public EventListFilter Filter { get; init; } = new();
    public PaginatedResult<EventSummaryDto> Events { get; init; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; init; } = [];
    public string? ErrorMessage { get; init; }
}

public sealed class AdminAttendeesViewModel
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public PaginatedResult<AdminEventRegistrationDto> Attendees { get; init; } = new();
}

public sealed class AdminEventRegistrationDto
{
    public Guid RegistrationId { get; init; }
    public Guid AttendeeId { get; init; }
    public string AttendeeName { get; init; } = string.Empty;
    public string AttendeeEmail { get; init; } = string.Empty;
    public DateTime RegistrationDate { get; init; }
    public string Status { get; init; } = string.Empty;
}
