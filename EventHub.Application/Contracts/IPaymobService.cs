
namespace EventHub.Application.Contracts
{
    public record PaymobPaymentRequest(
            Guid RegistrationId,
            decimal Amount,
            string FirstNAme,
            string LastName,
            string PhoneNumber,
            string Email,
            string MerchantOrderId
    );

    public record PaymobPaymentResponse(
        string PaymentUrl,
        string PaymobOrderId
    );

    public interface IPaymobService
    {
        Task<PaymobPaymentResponse> GeneratePaymentLinkAsync(PaymobPaymentRequest request, CancellationToken cancellationToken);
    }
}
