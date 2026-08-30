using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Domin.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Application.Features.Registerations.CancelRegistrationForEvent
{
    public class CancelRegistrationCommandHandler(
            IUnitOfWork _unitOfWork,
            IUserContext _userContext,
            IGenericRepository<Registration> _registrationRepository,
            IGenericRepository<Event> _eventRepository,
            IGenericRepository<PaymentTransaction> _transactionRepository
        ) : IRequestHandler<CancelRegistrationCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(CancelRegistrationCommand request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(RoleNames.Attendee))
                return RequestResult<bool>.Failure(ErrorCode.Forbidden);

            try
            {
                return await _unitOfWork.ExecuteAsync(async () =>
                {
                    var registration = await _registrationRepository.GetByIdAsTrackingAsync(request.RegistrationId, cancellationToken);
                    if (registration == null)
                        return RequestResult<bool>.Failure(ErrorCode.RegistrationNotFound, "Registration not found.");

                    if (registration.UserId != _userContext.UserId)
                        return RequestResult<bool>.Failure(ErrorCode.UnAuthorized, "You are not authorized to cancel this registration.");

                    if (registration.Status == RegistrationStatus.Canceled)
                        return RequestResult<bool>.Failure(ErrorCode.RegistrationAlreadyCanceled, "This registration is already canceled.");

                    if (registration.Status is not (RegistrationStatus.Pending or RegistrationStatus.Confirmed))
                        return RequestResult<bool>.Failure(ErrorCode.RegistrationClosed);

                    var @event = await _eventRepository.GetByIdAsTrackingAsync(registration.EventId, cancellationToken);
                    if (@event != null)
                    {
                        if (@event.Status == EventStatus.Completed || @event.EventDate <= DateTime.UtcNow)
                            return RequestResult<bool>.Failure(ErrorCode.RegistrationClosed, "Completed events can no longer be canceled.");

                        @event.DecrementAttendees();
                    }

                    registration.Status = RegistrationStatus.Canceled;
                    registration.UpdatedAt = DateTime.UtcNow;

                    var pendingTransaction = await _transactionRepository.FirstOrDefaultAsTrackingAsync(
                        transaction => transaction.RegistrationId == registration.Id && transaction.Status == PaymentTransactionStatus.Pending,
                        cancellationToken);
                    if (pendingTransaction is not null)
                    {
                        pendingTransaction.Status = PaymentTransactionStatus.Canceled;
                        pendingTransaction.UpdatedAt = DateTime.UtcNow;
                    }

                    return RequestResult<bool>.Success(true);
                }, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return RequestResult<bool>.Failure(ErrorCode.ConcurrencyConflict, "High traffic, please try again to cancel your registration.");
            }
            catch (Exception)
            {
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError, "An error occurred while canceling the registration.");
            }
        }
    }
}
