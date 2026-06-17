using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Application.Features.Registerations.RegisterationForEvent
{
    public class RegisterForEventCommandHandler(
          IUnitOfWork _unitOfWork,
          IUserContext _userContext,
          IGenericRepository<Event> _eventRepository,
          IGenericRepository<Registration> _registrationRepository
      ) : IRequestHandler<RegisterationCommand, RequestResult<Guid>>
    {
        public async Task<RequestResult<Guid>> Handle(RegisterationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteAsync(async () =>
                {
                    var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                    if (@event == null)
                        return RequestResult<Guid>.Failure(ErrorCode.EventNotFound, "Event Not Found");

                    if (!@event.TryIncrementAttendees())
                    {
                        return RequestResult<Guid>.Failure(ErrorCode.EventIsFull, "Sory, Events Is Full");
                    }

                    var registration = new Registration
                    {
                        EventId = @event.Id,
                        UserId = _userContext.UserId,
                        RegistrationDate = DateTime.UtcNow,
                        Status = RegistrationStatus.Confirmed
                    };

                    await _registrationRepository.AddAsync(registration, cancellationToken);

                    return RequestResult<Guid>.Success(registration.Id);
                }, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return RequestResult<Guid>.Failure(ErrorCode.ConcurrencyConflict, "High booking volume is currently unavailable, please try again");
            }
            catch (Exception)
            {
                return RequestResult<Guid>.Failure(ErrorCode.InternalServerError, "An unexpected error occurred during the registration process.");
            }
        }
    }
}
