using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Application.Features.Payments.CreatePaymobPaymentOrder;

/// <summary>
/// Dispatches a Paymob order only after the local payment intent has been committed.
/// A transaction left in Processing represents an externally uncertain attempt and is deliberately
/// not submitted again; its stable merchant-order reference allows webhook/operator reconciliation.
/// </summary>
public sealed class CreatePaymobPaymentOrderCommandHandler(
    IUnitOfWork unitOfWork,
    IGenericRepository<PaymentTransaction> transactionRepository,
    IAccountService accountService,
    IUserContext userContext,
    IPaymobService paymobService)
    : IRequestHandler<CreatePaymobPaymentOrderCommand, RequestResult<PaymobPaymentOrderResultDto>>
{
    public async Task<RequestResult<PaymobPaymentOrderResultDto>> Handle(
        CreatePaymobPaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsInRole(RoleNames.Attendee))
            return RequestResult<PaymobPaymentOrderResultDto>.Failure(ErrorCode.Forbidden);

        PaymentOrderClaim? claim;
        try
        {
            var claimResult = await unitOfWork.ExecuteAsync(async () =>
            {
                var paymentTransaction = await transactionRepository.GetAll()
                    .AsTracking()
                    .Include(transaction => transaction.Registration)
                    .OrderByDescending(transaction => transaction.CreatedAt)
                    .FirstOrDefaultAsync(
                        transaction => transaction.RegistrationId == request.RegistrationId,
                        cancellationToken);

                if (paymentTransaction is null || paymentTransaction.Registration.UserId != userContext.UserId)
                    return RequestResult<PaymentOrderClaim?>.Failure(ErrorCode.RegistrationNotFound);

                if (paymentTransaction.Status != PaymentTransactionStatus.Pending ||
                    paymentTransaction.Registration.Status != RegistrationStatus.Pending)
                {
                    return RequestResult<PaymentOrderClaim?>.Failure(
                        ErrorCode.PaymentProviderError,
                        "The registration is no longer eligible for payment.");
                }

                if (paymentTransaction.OrderCreationStatus == PaymentOrderCreationStatus.Created)
                {
                    return RequestResult<PaymentOrderClaim?>.Success(new PaymentOrderClaim(
                        paymentTransaction.Id,
                        paymentTransaction.RegistrationId,
                        paymentTransaction.MerchantOrderId,
                        paymentTransaction.Amount,
                        paymentTransaction.PaymentUrl,
                        IsExistingOrder: true));
                }

                if (paymentTransaction.OrderCreationStatus == PaymentOrderCreationStatus.Processing)
                {
                    return RequestResult<PaymentOrderClaim?>.Failure(
                        ErrorCode.PaymentProviderError,
                        "Payment order creation is awaiting reconciliation. Please do not submit another payment.");
                }

                paymentTransaction.OrderCreationStatus = PaymentOrderCreationStatus.Processing;
                paymentTransaction.OrderCreationAttempts++;
                paymentTransaction.OrderCreationLastAttemptAt = DateTime.UtcNow;
                paymentTransaction.OrderCreationFailureReason = null;
                paymentTransaction.UpdatedAt = DateTime.UtcNow;

                return RequestResult<PaymentOrderClaim?>.Success(new PaymentOrderClaim(
                    paymentTransaction.Id,
                    paymentTransaction.RegistrationId,
                    paymentTransaction.MerchantOrderId,
                    paymentTransaction.Amount,
                    PaymentUrl: null,
                    IsExistingOrder: false));
            }, cancellationToken);

            if (!claimResult.IsSuccess || claimResult.Data is null)
                return RequestResult<PaymobPaymentOrderResultDto>.Failure(claimResult.ErrorCode, claimResult.Message!);

            claim = claimResult.Data;
        }
        catch (DbUpdateException)
        {
            return RequestResult<PaymobPaymentOrderResultDto>.Failure(ErrorCode.DatabaseError, "Unable to prepare payment order creation.");
        }

        if (claim.IsExistingOrder)
        {
            return string.IsNullOrWhiteSpace(claim.PaymentUrl)
                ? RequestResult<PaymobPaymentOrderResultDto>.Failure(ErrorCode.PaymentProviderError, "The payment link is unavailable. Please contact support.")
                : RequestResult<PaymobPaymentOrderResultDto>.Success(new PaymobPaymentOrderResultDto(claim.RegistrationId, claim.PaymentUrl));
        }

        var profileResult = await accountService.GetUserProfileAsync(userContext.UserId.ToString(), cancellationToken);
        if (!profileResult.IsSuccess || profileResult.Data is null ||
            string.IsNullOrWhiteSpace(profileResult.Data.Email) || string.IsNullOrWhiteSpace(profileResult.Data.PhoneNumber))
        {
            await MarkCreationFailedAsync(claim.PaymentTransactionId, "Attendee billing information is unavailable.", cancellationToken);
            return RequestResult<PaymobPaymentOrderResultDto>.Failure(
                ErrorCode.ValidationError,
                "A confirmed email address and phone number are required for paid event registration.");
        }

        PaymobPaymentResponse providerResponse;
        try
        {
            providerResponse = await paymobService.GeneratePaymentLinkAsync(
                BuildPaymentRequest(claim, profileResult.Data),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await MarkCreationFailedAsync(claim.PaymentTransactionId, "Paymob order creation failed.", cancellationToken);
            return RequestResult<PaymobPaymentOrderResultDto>.Failure(ErrorCode.PaymentProviderError);
        }

        try
        {
            var persistResult = await PersistProviderOrderAsync(claim.PaymentTransactionId, providerResponse, cancellationToken);
            if (!persistResult.IsSuccess || persistResult.Data is null)
                return RequestResult<PaymobPaymentOrderResultDto>.Failure(persistResult.ErrorCode, persistResult.Message!);

            return RequestResult<PaymobPaymentOrderResultDto>.Success(persistResult.Data);
        }
        catch (DbUpdateException)
        {
            // The committed Processing intent and MerchantOrderId are retained for webhook reconciliation.
            return RequestResult<PaymobPaymentOrderResultDto>.Failure(
                ErrorCode.DatabaseError,
                "The payment order was created but is awaiting reconciliation. Please do not submit another payment.");
        }
    }

    private async Task<RequestResult<PaymobPaymentOrderResultDto>> PersistProviderOrderAsync(
        Guid paymentTransactionId,
        PaymobPaymentResponse providerResponse,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(async () =>
        {
            var paymentTransaction = await transactionRepository.GetByIdAsTrackingAsync(paymentTransactionId, cancellationToken);
            if (paymentTransaction is null)
                return RequestResult<PaymobPaymentOrderResultDto>.Failure(ErrorCode.TransactionNotFound);

            if (paymentTransaction.OrderCreationStatus != PaymentOrderCreationStatus.Processing)
                return RequestResult<PaymobPaymentOrderResultDto>.Failure(
                    ErrorCode.PaymentProviderError,
                    "The payment order cannot be safely finalized.");

            paymentTransaction.PaymobOrderId = providerResponse.PaymobOrderId;
            paymentTransaction.PaymentUrl = providerResponse.PaymentUrl;
            paymentTransaction.OrderCreationStatus = PaymentOrderCreationStatus.Created;
            paymentTransaction.OrderCreationFailureReason = null;
            paymentTransaction.UpdatedAt = DateTime.UtcNow;

            return RequestResult<PaymobPaymentOrderResultDto>.Success(
                new PaymobPaymentOrderResultDto(paymentTransaction.RegistrationId, providerResponse.PaymentUrl));
        }, cancellationToken);
    }

    private Task MarkCreationFailedAsync(Guid paymentTransactionId, string reason, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var paymentTransaction = await transactionRepository.GetByIdAsTrackingAsync(paymentTransactionId, cancellationToken);
            if (paymentTransaction is null)
                return RequestResult<bool>.Failure(ErrorCode.TransactionNotFound);

            if (paymentTransaction.OrderCreationStatus == PaymentOrderCreationStatus.Processing)
            {
                paymentTransaction.OrderCreationStatus = PaymentOrderCreationStatus.Failed;
                paymentTransaction.OrderCreationFailureReason = reason;
                paymentTransaction.UpdatedAt = DateTime.UtcNow;
            }

            return RequestResult<bool>.Success(true);
        }, cancellationToken);

    private static PaymobPaymentRequest BuildPaymentRequest(PaymentOrderClaim claim, UserProfileResponse profile)
    {
        var nameParts = profile.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.FirstOrDefault() ?? "Attendee";
        var lastName = nameParts.Skip(1).FirstOrDefault() ?? firstName;

        return new PaymobPaymentRequest(
            claim.RegistrationId,
            claim.Amount,
            firstName,
            lastName,
            profile.PhoneNumber!,
            profile.Email,
            claim.MerchantOrderId);
    }

    private sealed record PaymentOrderClaim(
        Guid PaymentTransactionId,
        Guid RegistrationId,
        string MerchantOrderId,
        decimal Amount,
        string? PaymentUrl,
        bool IsExistingOrder);
}
