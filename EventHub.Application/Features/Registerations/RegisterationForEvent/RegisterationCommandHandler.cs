using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Notifications.QueueRegistrationConfirmation;
using EventHub.Application.Features.Payments.CreatePaymobPaymentOrder;
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
          IMediator _mediator
      ) : IRequestHandler<RegisterationCommand, RequestResult<RegistrationResultDto>>
    {
        public async Task<RequestResult<RegistrationResultDto>> Handle(RegisterationCommand request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(RoleNames.Attendee))
                return RequestResult<RegistrationResultDto>.Failure(ErrorCode.Forbidden);

            try
            {
                var preparationResult = await _unitOfWork.ExecuteAsync(async () =>
                {
                    var @event = await _eventRepository.GetByIdAsTrackingAsync(request.EventId, cancellationToken);
                    if (@event == null)
                        return RequestResult<PaymentRegistrationPreparation>.Failure(ErrorCode.EventNotFound, "Event Not Found");

                    var existingRegistration = await _registrationRepository.FirstOrDefaultAsTrackingAsync(
                        registration => registration.UserId == _userContext.UserId && registration.EventId == request.EventId,
                        cancellationToken);

                    bool isPaidEvent = @event.Price > 0;
                    if (existingRegistration is not null && existingRegistration.Status != RegistrationStatus.Canceled)
                    {
                        if (isPaidEvent && existingRegistration.Status == RegistrationStatus.Pending)
                            return RequestResult<PaymentRegistrationPreparation>.Success(new PaymentRegistrationPreparation(existingRegistration.Id, true));

                        return RequestResult<PaymentRegistrationPreparation>.Failure(ErrorCode.AlreadyRegistered);
                    }

                    var attendeeResult = isPaidEvent
                        ? await _accountService.GetUserProfileAsync(_userContext.UserId.ToString(), cancellationToken)
                        : null;
                    var attendee = attendeeResult?.Data;
                    if (isPaidEvent && (attendee is null || string.IsNullOrWhiteSpace(attendee.Email) || string.IsNullOrWhiteSpace(attendee.PhoneNumber)))
                        return RequestResult<PaymentRegistrationPreparation>.Failure(ErrorCode.ValidationError, "A confirmed email address and phone number are required for paid event registration.");

                    if (!@event.IsOpenForRegistration(DateTime.UtcNow))
                    {
                        if (@event.CurrentAttendeesCount >= @event.MaxAttendees)
                            return RequestResult<PaymentRegistrationPreparation>.Failure(ErrorCode.EventIsFull);

                        return RequestResult<PaymentRegistrationPreparation>.Failure(ErrorCode.RegistrationClosed);
                    }

                    if (!@event.TryIncrementAttendees())
                    {
                        return RequestResult<PaymentRegistrationPreparation>.Failure(ErrorCode.EventIsFull, "Sory, Events Is Full");
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

                    if (isPaidEvent)
                    {
                        // Persist the local payment intent before any irreversible provider call.
                        var paymentTransaction = new PaymentTransaction
                        {
                            RegistrationId = registration.Id,
                            Amount = @event.Price,
                            Currency = "EGP",
                            Status = PaymentTransactionStatus.Pending,
                            MerchantOrderId = string.Empty,
                            OrderCreationStatus = PaymentOrderCreationStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        };

                        paymentTransaction.MerchantOrderId = paymentTransaction.Id.ToString();

                        await _transactionRepository.AddAsync(paymentTransaction, cancellationToken);
                    }
                    else
                    {
                        var notificationResult = await _mediator.Send(
                            new QueueRegistrationConfirmationCommand(registration.Id),
                            cancellationToken);
                        if (!notificationResult.IsSuccess)
                            return RequestResult<PaymentRegistrationPreparation>.Failure(notificationResult.ErrorCode, notificationResult.Message!);
                    }

                    return RequestResult<PaymentRegistrationPreparation>.Success(
                        new PaymentRegistrationPreparation(registration.Id, isPaidEvent));

                }, cancellationToken);

                if (!preparationResult.IsSuccess || preparationResult.Data is null)
                    return RequestResult<RegistrationResultDto>.Failure(preparationResult.ErrorCode, preparationResult.Message!);

                if (!preparationResult.Data.IsPaidEvent)
                    return RequestResult<RegistrationResultDto>.Success(new RegistrationResultDto(preparationResult.Data.RegistrationId));

                var paymentOrderResult = await _mediator.Send(
                    new CreatePaymobPaymentOrderCommand(preparationResult.Data.RegistrationId),
                    cancellationToken);
                if (!paymentOrderResult.IsSuccess || paymentOrderResult.Data is null)
                    return RequestResult<RegistrationResultDto>.Failure(paymentOrderResult.ErrorCode, paymentOrderResult.Message!);

                return RequestResult<RegistrationResultDto>.Success(
                    new RegistrationResultDto(preparationResult.Data.RegistrationId, paymentOrderResult.Data.PaymentUrl));
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

        private sealed record PaymentRegistrationPreparation(Guid RegistrationId, bool IsPaidEvent);
    }
}
