using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Notifications.QueueRegistrationConfirmation;
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
          IAccountService _accountService,
          IPaymobService _paymobService,
          IMediator _mediator
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

                    var existingRegistration = await _registrationRepository.FirstOrDefaultAsTrackingAsync(
                        registration => registration.UserId == _userContext.UserId && registration.EventId == request.EventId,
                        cancellationToken);
                    if (existingRegistration is not null && existingRegistration.Status != RegistrationStatus.Canceled)
                        return RequestResult<RegistrationResultDto>.Failure(ErrorCode.AlreadyRegistered);

                    bool isPaidEvent = @event.Price > 0;
                    var attendeeResult = isPaidEvent
                        ? await _accountService.GetUserProfileAsync(_userContext.UserId.ToString(), cancellationToken)
                        : null;
                    var attendee = attendeeResult?.Data;
                    if (isPaidEvent && (attendee is null || string.IsNullOrWhiteSpace(attendee.Email) || string.IsNullOrWhiteSpace(attendee.PhoneNumber)))
                        return RequestResult<RegistrationResultDto>.Failure(ErrorCode.ValidationError, "A confirmed email address and phone number are required for paid event registration.");

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

                    var initialStatus = isPaidEvent ? RegistrationStatus.Pending : RegistrationStatus.Confirmed;

                    // Pending paid registrations reserve capacity until the existing payment workflow resolves them.

                    var registration = existingRegistration ?? new Registration
                    {
                        EventId = @event.Id,
                        UserId = _userContext.UserId,
                        CreatedAt = DateTime.UtcNow
                    };
                    registration.RegistrationDate = DateTime.UtcNow;
                    registration.Status = initialStatus;
                    registration.UpdatedAt = DateTime.UtcNow;

                    if (existingRegistration is null)
                        await _registrationRepository.AddAsync(registration, cancellationToken);

                    // Payment
                    string? paymentUrl = null;
                    if (isPaidEvent)
                    {
                        var nameParts = attendee!.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var firstName = nameParts.FirstOrDefault() ?? "Attendee";
                        var lastName = nameParts.Skip(1).FirstOrDefault() ?? firstName;
                        var paymobRequest = new PaymobPaymentRequest(
                            registration.Id,
                            @event.Price,
                            firstName,
                            lastName,
                            attendee.PhoneNumber!,
                            attendee.Email!
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
                    else
                    {
                        var notificationResult = await _mediator.Send(
                            new QueueRegistrationConfirmationCommand(registration.Id),
                            cancellationToken);
                        if (!notificationResult.IsSuccess)
                            return RequestResult<RegistrationResultDto>.Failure(notificationResult.ErrorCode, notificationResult.Message!);
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
