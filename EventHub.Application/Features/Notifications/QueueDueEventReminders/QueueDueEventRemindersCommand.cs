using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Notifications.QueueDueEventReminders
{
    public record QueueDueEventRemindersCommand : IRequest<RequestResult<int>>;
}
