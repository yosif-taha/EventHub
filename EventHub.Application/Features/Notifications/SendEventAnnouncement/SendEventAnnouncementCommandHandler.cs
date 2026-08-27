using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Notifications.QueueEventNotification;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Notifications.SendEventAnnouncement
{
    public class SendEventAnnouncementCommandHandler(
        IGenericRepository<Event> _eventRepository,
        IUserContext _userContext,
        IMediator _mediator) : IRequestHandler<SendEventAnnouncementCommand, RequestResult<int>>
    {
        public async Task<RequestResult<int>> Handle(SendEventAnnouncementCommand request, CancellationToken cancellationToken)
        {
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null)
                return RequestResult<int>.Failure(ErrorCode.EventNotFound);

            var isAdmin = _userContext.IsInRole(RoleNames.Admin);
            var isOrganizer = _userContext.IsInRole(RoleNames.Organizer);
            if (!isAdmin && (!isOrganizer || @event.OrganizerId != _userContext.UserId))
                return RequestResult<int>.Failure(ErrorCode.Forbidden);

            return await _mediator.Send(
                new QueueEventNotificationCommand(request.EventId, NotificationType.EventAnnouncement, request.Subject, request.Message),
                cancellationToken);
        }
    }
}
