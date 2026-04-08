namespace EventHub.WebAPI.Presentation.ViewModels.Events
{
    public record CheckEventAvailabilityViewModel(
      bool IsAvailable,
      int RemainingSlots,
      bool IsCancelled
    );

}
