
namespace EventHub.Application.Common.Dtos.Events
{
    public record EventAvailabilityDto(
      bool IsAvailable,
      int RemainingSlots,
      bool IsCancelled
    );
}
