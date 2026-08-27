using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Notifications.QueueDueEventReminders
{
    public class QueueDueEventRemindersCommandHandler(
        IGenericRepository<Event> _eventRepository,
        IGenericRepository<Registration> _registrationRepository,
        IGenericRepository<Notification> _notificationRepository,
        IDbExecutor _executor,
        IUnitOfWork _unitOfWork) : IRequestHandler<QueueDueEventRemindersCommand, RequestResult<int>>
    {
        private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

        public async Task<RequestResult<int>> Handle(QueueDueEventRemindersCommand request, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            var cutoff = utcNow.Add(ReminderWindow);
            var events = await _executor.ToListAsync(
                _eventRepository.GetAll()
                    .Where(@event => @event.Status == EventStatus.Scheduled && @event.EventDate > utcNow && @event.EventDate <= cutoff)
                    .Select(@event => new { @event.Id, @event.Title, @event.EventDate, @event.Location }),
                cancellationToken);

            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var queued = 0;
                foreach (var @event in events)
                {
                    var userIds = await _executor.ToListAsync(
                        _registrationRepository.GetAll()
                            .Where(registration => registration.EventId == @event.Id && registration.Status == RegistrationStatus.Confirmed)
                            .Select(registration => registration.UserId),
                        cancellationToken);

                    foreach (var userId in userIds.Distinct())
                    {
                        var deduplicationKey = $"event-reminder:{@event.Id}:{userId}";
                        if (await _notificationRepository.AnyAsync(notification => notification.DeduplicationKey == deduplicationKey, cancellationToken))
                            continue;

                        await _notificationRepository.AddAsync(new Notification
                        {
                            EventId = @event.Id,
                            UserId = userId,
                            Type = NotificationType.EventReminder,
                            Subject = $"Event reminder: {@event.Title}",
                            Message = $"Reminder: '{@event.Title}' starts on {@event.EventDate:yyyy-MM-dd HH:mm} at {@event.Location}.",
                            NotificationDate = DateTime.UtcNow,
                            DeduplicationKey = deduplicationKey,
                            CreatedAt = DateTime.UtcNow
                        }, cancellationToken);
                        queued++;
                    }
                }

                return RequestResult<int>.Success(queued);
            }, cancellationToken);
        }
    }
}
