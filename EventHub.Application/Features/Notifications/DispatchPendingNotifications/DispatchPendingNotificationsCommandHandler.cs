using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Notifications.DispatchPendingNotifications
{
    public class DispatchPendingNotificationsCommandHandler(
        IGenericRepository<Notification> _notificationRepository,
        IAccountService _accountService,
        IEmailService _emailService,
        IDbExecutor _executor,
        IUnitOfWork _unitOfWork) : IRequestHandler<DispatchPendingNotificationsCommand, RequestResult<int>>
    {
        private const int MaximumDeliveryAttempts = 3;
        private static readonly TimeSpan DeliveryLeaseDuration = TimeSpan.FromMinutes(15);

        public async Task<RequestResult<int>> Handle(DispatchPendingNotificationsCommand request, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            var notificationIds = await _executor.ToListAsync(
                _notificationRepository.GetAll()
                    .Where(notification =>
                        (notification.DeliveryStatus == NotificationDeliveryStatus.Pending || notification.DeliveryStatus == NotificationDeliveryStatus.Failed) &&
                        notification.DeliveryAttempts < MaximumDeliveryAttempts ||
                        notification.DeliveryStatus == NotificationDeliveryStatus.Processing &&
                        notification.DeliveryLeaseExpiresAt <= utcNow &&
                        notification.DeliveryAttempts < MaximumDeliveryAttempts)
                    .OrderBy(notification => notification.NotificationDate)
                    .Take(50)
                    .Select(notification => notification.Id),
                cancellationToken);

            var sent = 0;
            foreach (var notificationId in notificationIds)
            {
                var leaseId = Guid.NewGuid();
                if (!await TryClaimAsync(notificationId, leaseId, cancellationToken))
                    continue;

                var delivered = false;
                try
                {
                    var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);
                    if (notification is null)
                        continue;

                    var userResult = await _accountService.GetUserProfileAsync(notification.UserId.ToString(), cancellationToken);
                    if (userResult.IsSuccess && !string.IsNullOrWhiteSpace(userResult.Data?.Email))
                    {
                        await _emailService.SendEmailAsync(userResult.Data.Email, notification.Subject, notification.Message, cancellationToken);
                        delivered = true;
                    }
                }
                catch
                {
                    delivered = false;
                }

                var completed = await CompleteDeliveryAsync(notificationId, leaseId, delivered, cancellationToken);

                if (completed && delivered)
                    sent++;
            }

            return RequestResult<int>.Success(sent);
        }

        private async Task<bool> TryClaimAsync(Guid notificationId, Guid leaseId, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteAsync(async () =>
                {
                    var notification = await _notificationRepository.GetByIdAsTrackingAsync(notificationId, cancellationToken);
                    var utcNow = DateTime.UtcNow;
                    var canClaim = notification is not null &&
                        notification.DeliveryAttempts < MaximumDeliveryAttempts &&
                        (notification.DeliveryStatus is NotificationDeliveryStatus.Pending or NotificationDeliveryStatus.Failed ||
                         notification.DeliveryStatus == NotificationDeliveryStatus.Processing &&
                         notification.DeliveryLeaseExpiresAt <= utcNow);

                    if (!canClaim)
                        return false;

                    notification!.DeliveryStatus = NotificationDeliveryStatus.Processing;
                    notification.DeliveryAttempts++;
                    notification.LastAttemptAt = utcNow;
                    notification.DeliveryLeaseId = leaseId;
                    notification.DeliveryLeaseExpiresAt = utcNow.Add(DeliveryLeaseDuration);
                    return true;
                }, cancellationToken);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                return false;
            }
        }

        private Task<bool> CompleteDeliveryAsync(
            Guid notificationId,
            Guid leaseId,
            bool delivered,
            CancellationToken cancellationToken) =>
            _unitOfWork.ExecuteAsync(async () =>
            {
                var notification = await _notificationRepository.GetByIdAsTrackingAsync(notificationId, cancellationToken);
                if (notification is null || notification.DeliveryStatus != NotificationDeliveryStatus.Processing ||
                    notification.DeliveryLeaseId != leaseId)
                    return false;

                notification.DeliveryStatus = delivered
                    ? NotificationDeliveryStatus.Sent
                    : NotificationDeliveryStatus.Failed;
                notification.SentAt = delivered ? DateTime.UtcNow : null;
                notification.DeliveryLeaseId = null;
                notification.DeliveryLeaseExpiresAt = null;
                return true;
            }, cancellationToken);
    }
}
