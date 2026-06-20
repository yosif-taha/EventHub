using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Application.Features.Registerations.CancelRegistrationForEvent
{
    public class CancelRegistrationCommandHandler(
            IUnitOfWork _unitOfWork,
            IUserContext _userContext,
            IGenericRepository<Registration> _registrationRepository,
            IGenericRepository<Event> _eventRepository
        ) : IRequestHandler<CancelRegistrationCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(CancelRegistrationCommand request, CancellationToken cancellationToken)
        {
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

                    var @event = await _eventRepository.GetByIdAsTrackingAsync(registration.EventId, cancellationToken);
                    if (@event != null)
                    {
                        @event.DecrementAttendees();
                    }

                    registration.Status = RegistrationStatus.Canceled;

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
