using EventHub.Application.Common.Responses;
using EventHub.Domin.Enums;
using MediatR;

namespace EventHub.Application.Features.Notifications.QueueEventNotification
{
    public record QueueEventNotificationCommand(
        Guid EventId,
        NotificationType Type,
        string Subject,
        string Message) : IRequest<RequestResult<int>>;
}
