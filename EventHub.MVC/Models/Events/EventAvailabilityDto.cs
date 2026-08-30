namespace EventHub.MVC.Models.Events;

public sealed class EventAvailabilityDto
{
    public bool IsAvailable { get; init; }
    public int RemainingSlots { get; init; }
    public bool IsCancelled { get; init; }
}
