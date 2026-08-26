using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Domin.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Application.Features.Registerations.RegisterationForEvent
{
    public class RegisterForEventCommandHandler(
          IUnitOfWork _unitOfWork,
          IUserContext _userContext,
          IGenericRepository<Event> _eventRepository,
          IGenericRepository<PaymentTransaction> _transactionRepository,
          IGenericRepository<Registration> _registrationRepository,
          IPaymobService _paymobService
      ) : IRequestHandler<RegisterationCommand, RequestResult<RegistrationResultDto>>
    {
        public async Task<RequestResult<RegistrationResultDto>> Handle(RegisterationCommand request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(RoleNames.Attendee))
                return RequestResult<RegistrationResultDto>.Failure(ErrorCode.Forbidden);

            try
            {
                return await _unitOfWork.ExecuteAsync(async () =>
                {
                    var @event = await _eventRepository.GetByIdAsTrackingAsync(request.EventId, cancellationToken);
                    if (@event == null)
                        return RequestResult<RegistrationResultDto>.Failure(ErrorCode.EventNotFound, "Event Not Found");

                    var alreadyRegistered = await _registrationRepository.AnyAsync(
                        registration => registration.UserId == _userContext.UserId && registration.EventId == request.EventId,
                        cancellationToken);
                    if (alreadyRegistered)
                        return RequestResult<RegistrationResultDto>.Failure(ErrorCode.AlreadyRegistered);

                    if (!@event.IsOpenForRegistration(DateTime.UtcNow))
                    {
                        if (@event.CurrentAttendeesCount >= @event.MaxAttendees)
                            return RequestResult<RegistrationResultDto>.Failure(ErrorCode.EventIsFull);

                        return RequestResult<RegistrationResultDto>.Failure(ErrorCode.RegistrationClosed);
                    }

                    if (!@event.TryIncrementAttendees())
                    {
                        return RequestResult<RegistrationResultDto>.Failure(ErrorCode.EventIsFull, "Sory, Events Is Full");
                    }

                    bool isPaidEvent = @event.Price > 0;
                    var initialStatus = isPaidEvent ? RegistrationStatus.Pending : RegistrationStatus.Confirmed;

                    // Pending paid registrations reserve capacity until the existing payment workflow resolves them.

                    var registration = new Registration
                    {
                        EventId = @event.Id,
                        UserId = _userContext.UserId,
                        RegistrationDate = DateTime.UtcNow,
                        Status = initialStatus,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _registrationRepository.AddAsync(registration, cancellationToken);

                    // Payment
                    string? paymentUrl = null;
                    if (isPaidEvent)
                    {
                        var paymobRequest = new PaymobPaymentRequest(
                            registration.Id,
                            @event.Price,
                            _userContext.Email,
                            _userContext.Email,
                            _userContext.Email,
                            _userContext.Email
                        );

                        var paymobResponse = await _paymobService.GeneratePaymentLinkAsync(paymobRequest, cancellationToken);
                        paymentUrl = paymobResponse.PaymentUrl;

                        var paymentTransaction = new PaymentTransaction
                        {
                            RegistrationId = registration.Id,
                            Amount = @event.Price,
                            Currency = "EGP",
                            Status = PaymentTransactionStatus.Pending,
                            PaymobOrderId = paymobResponse.PaymobOrderId, // For Webhook
                            CreatedAt = DateTime.UtcNow
                        };

                        await _transactionRepository.AddAsync(paymentTransaction, cancellationToken);
                    }

                    // Final Result
                    var resultDto = new RegistrationResultDto(registration.Id, paymentUrl);
                    return RequestResult<RegistrationResultDto>.Success(resultDto);

                }, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return RequestResult<RegistrationResultDto>.Failure(ErrorCode.ConcurrencyConflict, "High booking volume is currently unavailable, please try again");
            }
            catch (Exception)
            {
                return RequestResult<RegistrationResultDto>.Failure(ErrorCode.InternalServerError, "An unexpected error occurred during the registration process.");
            }
        }
    }
}
