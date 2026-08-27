using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Notifications.SendEventAnnouncement
{
    public record SendEventAnnouncementCommand(Guid EventId, string Subject, string Message) : IRequest<RequestResult<int>>;
}
