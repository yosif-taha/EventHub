namespace EventHub.WebAPI.Presentation.ViewModels.Notifications
{
    public record SendEventAnnouncementRequest(Guid EventId, string Subject, string Message);
}
