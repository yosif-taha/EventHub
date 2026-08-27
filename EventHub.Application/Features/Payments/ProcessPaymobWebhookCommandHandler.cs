using EventHub.Application.Common.Responses;
using EventHub.Application.Common.Dtos.Registrations.Payments;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Notifications.QueueRegistrationConfirmation;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Application.Features.Payments
{
    public class ProcessPaymobWebhookCommandHandler(
            IUnitOfWork _unitOfWork,
            IGenericRepository<Registration> _registrationRepository,
            IGenericRepository<PaymentTransaction> _transactionRepository,
            IGenericRepository<Event> _eventRepository,
            IMediator _mediator
        ) : IRequestHandler<ProcessPaymobWebhookCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(ProcessPaymobWebhookCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteAsync(async () =>
                {
                    var transactionData = request.Payload;

                    if (transactionData.Order is null || transactionData.Order.Id <= 0 || transactionData.Id <= 0)
                        return RequestResult<bool>.Failure(ErrorCode.PaymentProviderError, "The Paymob callback is incomplete.");

                    string paymobOrderId = transactionData.Order.Id.ToString();
                    string paymobTransactionId = transactionData.Id.ToString();
                    bool isSuccess = transactionData.Success;

                    var transaction = await _transactionRepository.FirstOrDefaultAsTrackingAsync(
                        paymentTransaction => paymentTransaction.PaymobOrderId == paymobOrderId,
                        cancellationToken);

                    if (transaction == null)
                        return RequestResult<bool>.Failure(ErrorCode.TransactionNotFound, "Transaction not found.");

                    var registration = await _registrationRepository.GetByIdAsTrackingAsync(transaction.RegistrationId, cancellationToken);
                    if (registration == null)
                        return RequestResult<bool>.Failure(ErrorCode.RegistrationNotFound, "Associated registration not found.");

                    if (transactionData.Order.MerchantOrderId != registration.Id.ToString() ||
                        transactionData.AmountCents != decimal.ToInt64(transaction.Amount * 100) ||
                        !string.Equals(transactionData.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
                        return RequestResult<bool>.Failure(ErrorCode.PaymentProviderError, "The Paymob callback does not match the pending payment transaction.");

                    if (transactionData.Pending)
                        return RequestResult<bool>.Success(true);

                    if (IsCallbackOutcomePersisted(transaction, transactionData, paymobTransactionId))
                        return RequestResult<bool>.Success(true);

                    if (transaction.Status == PaymentTransactionStatus.Canceled)
                    {
                        transaction.PaymobTransactionId = paymobTransactionId;
                        if (isSuccess)
                            transaction.Status = PaymentTransactionStatus.Success;
                        transaction.UpdatedAt = DateTime.UtcNow;
                        return RequestResult<bool>.Success(true);
                    }

                    if (transaction.Status != PaymentTransactionStatus.Pending)
                        return RequestResult<bool>.Failure(
                            ErrorCode.PaymentProviderError,
                            "The Paymob callback conflicts with the recorded payment transaction.");

                    // Update Data Of Transaction Table
                    transaction.PaymobTransactionId = paymobTransactionId;
                    transaction.UpdatedAt = DateTime.UtcNow;

                    if (isSuccess)
                    {
                        transaction.Status = PaymentTransactionStatus.Success;
                        if (registration.Status == RegistrationStatus.Pending)
                        {
                            registration.Status = RegistrationStatus.Confirmed;
                            var notificationResult = await _mediator.Send(
                                new QueueRegistrationConfirmationCommand(registration.Id),
                                cancellationToken);
                            if (!notificationResult.IsSuccess)
                                return RequestResult<bool>.Failure(notificationResult.ErrorCode, notificationResult.Message!);
                        }
                    }
                    else
                    {
                        transaction.Status = PaymentTransactionStatus.Failed;
                        if (registration.Status == RegistrationStatus.Pending)
                        {
                            registration.Status = RegistrationStatus.Canceled;
                            var @event = await _eventRepository.GetByIdAsTrackingAsync(registration.EventId, cancellationToken);
                            @event?.DecrementAttendees();
                        }
                    }

                    return RequestResult<bool>.Success(true);
                }, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return await GetConcurrencyResultAsync(request.Payload, cancellationToken);
            }
            catch (DbUpdateException)
            {
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError, "Unable to persist the payment callback. Please retry.");
            }
            catch (Exception)
            {
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError, "An error occurred while processing payment webhook.");
            }
        }

        private async Task<RequestResult<bool>> GetConcurrencyResultAsync(
            PaymobTransactionObj callback,
            CancellationToken cancellationToken)
        {
            if (callback.Order is null || callback.Order.Id <= 0 || callback.Id <= 0)
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError, "Unable to verify the payment callback outcome. Please retry.");

            var persistedTransaction = await _transactionRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    transaction => transaction.PaymobOrderId == callback.Order.Id.ToString(),
                    cancellationToken);

            if (persistedTransaction is not null && IsCallbackOutcomePersisted(
                    persistedTransaction,
                    callback,
                    callback.Id.ToString()))
                return RequestResult<bool>.Success(true);

            return RequestResult<bool>.Failure(
                ErrorCode.InternalServerError,
                "The payment callback was not persisted. Please retry.");
        }

        private static bool IsCallbackOutcomePersisted(
            PaymentTransaction transaction,
            PaymobTransactionObj callback,
            string paymobTransactionId)
        {
            if (callback.Pending)
                return transaction.Status == PaymentTransactionStatus.Pending;

            if (!string.Equals(transaction.PaymobTransactionId, paymobTransactionId, StringComparison.Ordinal))
                return false;

            return callback.Success
                ? transaction.Status == PaymentTransactionStatus.Success
                : transaction.Status is PaymentTransactionStatus.Failed or PaymentTransactionStatus.Canceled;
        }
    }
}
