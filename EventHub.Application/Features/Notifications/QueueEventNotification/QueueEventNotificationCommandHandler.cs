using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Notifications.QueueEventNotification
{
    public class QueueEventNotificationCommandHandler(
        IGenericRepository<Event> _eventRepository,
        IGenericRepository<Registration> _registrationRepository,
        IGenericRepository<Notification> _notificationRepository,
        IDbExecutor _executor,
        IUnitOfWork _unitOfWork) : IRequestHandler<QueueEventNotificationCommand, RequestResult<int>>
    {
        public async Task<RequestResult<int>> Handle(QueueEventNotificationCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event is null)
                    return RequestResult<int>.Failure(ErrorCode.EventNotFound);

                var userIds = await _executor.ToListAsync(
                    _registrationRepository.GetAll()
                        .Where(registration => registration.EventId == request.EventId && registration.Status == RegistrationStatus.Confirmed)
                        .Select(registration => registration.UserId),
                    cancellationToken);

                foreach (var userId in userIds.Distinct())
                {
                    var deduplicationKey = request.Type == NotificationType.EventReminder
                        ? $"event-reminder:{request.EventId}:{userId}"
                        : null;

                    if (deduplicationKey is not null && await _notificationRepository.AnyAsync(
                        notification => notification.DeduplicationKey == deduplicationKey,
                        cancellationToken))
                        continue;

                    await _notificationRepository.AddAsync(new Notification
                    {
                        EventId = request.EventId,
                        UserId = userId,
                        Type = request.Type,
                        Subject = request.Subject,
                        Message = request.Message,
                        NotificationDate = DateTime.UtcNow,
                        DeduplicationKey = deduplicationKey,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }

                return RequestResult<int>.Success(userIds.Count);
            }, cancellationToken);
        }
    }
}
