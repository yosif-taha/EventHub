using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Notifications.QueueRegistrationConfirmation
{
    public class QueueRegistrationConfirmationCommandHandler(
        IGenericRepository<Registration> _registrationRepository,
        IGenericRepository<Event> _eventRepository,
        IGenericRepository<Notification> _notificationRepository,
        IUnitOfWork _unitOfWork) : IRequestHandler<QueueRegistrationConfirmationCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(QueueRegistrationConfirmationCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var registration = await _registrationRepository.GetByIdAsTrackingAsync(request.RegistrationId, cancellationToken);
                if (registration is null)
                    return RequestResult<bool>.Failure(ErrorCode.RegistrationNotFound);

                if (registration.Status != RegistrationStatus.Confirmed)
                    return RequestResult<bool>.Success(false);

                var @event = await _eventRepository.GetByIdAsync(registration.EventId, cancellationToken);
                if (@event is null)
                    return RequestResult<bool>.Failure(ErrorCode.EventNotFound);

                var deduplicationKey = $"registration-confirmation:{registration.Id}";
                if (await _notificationRepository.AnyAsync(notification => notification.DeduplicationKey == deduplicationKey, cancellationToken))
                    return RequestResult<bool>.Success(false);

                await _notificationRepository.AddAsync(new Notification
                {
                    EventId = @event.Id,
                    UserId = registration.UserId,
                    Type = NotificationType.RegistrationConfirmation,
                    Subject = $"Registration confirmed: {@event.Title}",
                    Message = $"Your registration for '{@event.Title}' on {@event.EventDate:yyyy-MM-dd HH:mm} has been confirmed.",
                    NotificationDate = DateTime.UtcNow,
                    DeduplicationKey = deduplicationKey,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                return RequestResult<bool>.Success(true);
            }, cancellationToken);
        }
    }
}
