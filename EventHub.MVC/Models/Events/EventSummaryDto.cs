using EventHub.Domin.Enums;

namespace EventHub.MVC.Models.Events;

public class EventSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public int MaxAttendees { get; init; }
    public int CurrentAttendeesCount { get; init; }
    public int RemainingSlots { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool PaymentRequired { get; init; }
    public decimal Price { get; init; }
    public EventMode Mode { get; init; }
    public string CategoryName { get; init; } = string.Empty;
}
