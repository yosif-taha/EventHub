using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Payments
{
    public class ProcessPaymobWebhookCommandHandler(
            IUnitOfWork _unitOfWork,
            IGenericRepository<Registration> _registrationRepository,
            IGenericRepository<PaymentTransaction> _transactionRepository,
            IDbExecutor _executor
        ) : IRequestHandler<ProcessPaymobWebhookCommand, RequestResult<bool>>
    {
        public async Task<RequestResult<bool>> Handle(ProcessPaymobWebhookCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteAsync(async () =>
                {
                    var transactionData = request.Payload;

                    string paymobOrderId =transactionData.Order.Id.ToString();
                    string paymobTransactionId = transactionData.Id.ToString();
                    bool isSuccess = transactionData.Success;

                    var query = _transactionRepository.GetAll();
                    var transaction = await _executor.FirstOrDefaultAsync(query, x => x.PaymobOrderId == paymobOrderId,cancellationToken);

                    if (transaction == null)
                        return RequestResult<bool>.Failure(ErrorCode.TransactionNotFound, "Transaction not found.");

                    if (transaction.Status != PaymentTransactionStatus.Pending)
                        return RequestResult<bool>.Failure(ErrorCode.TransactionAlreadyProcessed, "Transaction already.");

                    var registration = await _registrationRepository.GetByIdAsTrackingAsync(transaction.RegistrationId, cancellationToken);
                    if (registration == null)
                        return RequestResult<bool>.Failure(ErrorCode.RegistrationNotFound, "Associated registration not found.");

                    // Update Data Of Transaction Table
                    transaction.PaymobTransactionId = paymobTransactionId;
                    transaction.UpdatedAt = DateTime.UtcNow;

                    if (isSuccess)
                    {
                        transaction.Status = PaymentTransactionStatus.Success;
                        registration.Status = RegistrationStatus.Confirmed; 
                    }
                    else
                    {
                        transaction.Status = PaymentTransactionStatus.Failed;
                        registration.Status = RegistrationStatus.Canceled;

                    }

                    return RequestResult<bool>.Success(true);
                }, cancellationToken);
            }
            catch (Exception)
            {
                return RequestResult<bool>.Failure(ErrorCode.InternalServerError, "An error occurred while processing payment webhook.");
            }
        }
    }
}
