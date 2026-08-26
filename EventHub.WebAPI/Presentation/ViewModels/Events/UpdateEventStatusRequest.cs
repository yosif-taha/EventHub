using EventHub.Domin.Enums;

namespace EventHub.WebAPI.Presentation.ViewModels.Events
{
    public record UpdateEventStatusRequest(EventStatus Status);
}
