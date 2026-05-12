namespace EventHub.WebAPI.Presentation.ViewModels.Events
{
    public record CreateEventRequest(
    string Title,
    string Description,
    DateTime EventDate,
    string Location,
    Guid CategoryId,
    int MaxAttendees);

}
