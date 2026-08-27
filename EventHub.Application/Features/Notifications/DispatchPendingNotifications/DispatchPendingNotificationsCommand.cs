using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Notifications.DispatchPendingNotifications
{
    public record DispatchPendingNotificationsCommand : IRequest<RequestResult<int>>;
}
