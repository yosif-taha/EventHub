namespace EventHub.WebAPI.Presentation.ViewModels.Events
{
    public record CreateEventRequest(
    string Title,
    string Description,
    DateTime EventDate,
    double Price,
    string Location,
    Guid CategoryId,
    int MaxAttendees);

}
