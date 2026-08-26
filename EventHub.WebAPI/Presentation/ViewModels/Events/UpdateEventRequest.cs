namespace EventHub.WebAPI.Presentation.ViewModels.Events
{
    public record UpdateEventRequest
    (
        Guid Id,
      string? Title,
      string? Description,
      DateTime? EventDate,
      string? Location,
      Guid? CategoryId,
      int? MaxAttendees,
      EventHub.Domin.Enums.EventMode? Mode,
      string? OnlineMeetingUrl
    );
}
