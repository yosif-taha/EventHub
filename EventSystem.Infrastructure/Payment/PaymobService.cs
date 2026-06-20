using EventHub.Application.Contracts;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace EventHub.Infrastructure.Payment
{
    public class PaymobService(HttpClient _httpClient, IOptions<PaymobSettings> _settings) : IPaymobService
    {
        private readonly PaymobSettings _paymobSettings = _settings.Value;

        public async Task<PaymobPaymentResponse> GeneratePaymentLinkAsync(PaymobPaymentRequest request, CancellationToken cancellationToken)
        {
            // Authentication 
            var authResponse = await _httpClient.PostAsJsonAsync("auth/tokens", new { api_key = _paymobSettings.ApiKey }, cancellationToken);
            authResponse.EnsureSuccessStatusCode();
            var authData = await authResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
            string authToken = authData!["token"]!.ToString();

            // Order Registration 
            var orderPayload = new
            {
                auth_token = authToken,
                delivery_needed = "false",
                amount_cents = (int)(request.Amount * 100), 
                currency = "EGP",
                merchant_order_id = request.RegistrationId.ToString(), 
                items = new[] { new { name = "Event Ticket", amount_cents = (int)(request.Amount * 100), quantity = 1 } }
            };

            var orderResponse = await _httpClient.PostAsJsonAsync("ecommerce/orders", orderPayload, cancellationToken);
            orderResponse.EnsureSuccessStatusCode();
            var orderData = await orderResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
            string paymobOrderId = orderData!["id"]!.ToString();

            //  Payment Key Generation (PaymentToken)
            var paymentKeyPayload = new
            {
                auth_token = authToken,
                amount_cents = (int)(request.Amount * 100),
                expiration = (3600 * 3),
                order_id = paymobOrderId,
                billing_data = new
                {
                    first_name = request.FirstNAme,      
                    last_name = request.LastName,
                    phone_number = request.PhoneNumber,
                    email = request.Email,
                    apartment = "NA",
                    floor = "NA",
                    building = "NA",
                    street = "NA",
                    city = "NA",
                    country = "EG" 
                },
                currency = "EGP",
                integration_id = _paymobSettings.CardIntegrationId 
            };

            var paymentKeyResponse = await _httpClient.PostAsJsonAsync("acceptance/payment_keys", paymentKeyPayload, cancellationToken);
            paymentKeyResponse.EnsureSuccessStatusCode();
            var paymentKeyData = await paymentKeyResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
            string paymentToken = paymentKeyData!["token"]!.ToString();

            // Final Step (Return URL)
            string paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_paymobSettings.IframeId}?payment_token={paymentToken}";
            return new PaymobPaymentResponse(paymentUrl, paymobOrderId);
        }
    }
}
