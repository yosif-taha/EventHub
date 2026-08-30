using EventHub.Domin.Enums;

namespace EventHub.MVC.Models.Events;

public sealed class EventDetailsDto : EventSummaryDto
{
    public string? OnlineMeetingUrl { get; init; }
}
