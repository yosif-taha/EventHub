using EventHub.Application.Common.Dtos.Registrations.Payments;
using EventHub.Application.Features.Payments;
using EventHub.Infrastructure.Payment;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PaymentController(IMediator _mediator, IOptions<PaymobSettings> _settings) : ControllerBase
    {
        private readonly PaymobSettings _paymobSettings = _settings.Value;

        [HttpPost]
        [HttpPost("/api/payments/paymob/webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] JsonObject? rawJsonPayload, [FromQuery] string? hmac, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_paymobSettings.HmacSecret))
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Payment webhook is not configured.");

            if (rawJsonPayload is null)
                return BadRequest("The Paymob callback is incomplete.");

            if (!ValidateHmac(rawJsonPayload, hmac))
                return Unauthorized("Invalid HMAC signature.");

            var transactionObject = rawJsonPayload["obj"] as JsonObject;
            if (transactionObject is null)
                return BadRequest("The Paymob callback is incomplete.");

            var payload = transactionObject.Deserialize<PaymobTransactionObj>(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            if (payload is null || payload.Id <= 0 || payload.Order is null || payload.Order.Id <= 0)
                return BadRequest("The Paymob callback is incomplete.");

            var result = await _mediator.Send(new ProcessPaymobWebhookCommand(payload), ct);

            if (!result.IsSuccess)
            {
                if (result.ErrorCode == EventHub.Application.Common.Responses.ErrorCode.PaymentProviderError)
                    return BadRequest(result.Message);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    "The payment callback could not be processed. Please retry.");
            }

            return Ok();
        }

        private bool ValidateHmac(JsonObject payload, string? receivedHmac)
        {
            if (string.IsNullOrWhiteSpace(_paymobSettings.HmacSecret) || string.IsNullOrWhiteSpace(receivedHmac))
                return false;

            var obj = payload["obj"] as JsonObject;
            if (obj == null) return false;

            string[] keys = ["amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction", "id", "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded", "is_standalone_payment", "is_voided", "order.id", "owner", "pending", "source_data.pan", "source_data.sub_type", "source_data.type", "success"];

            var stringBuilder = new StringBuilder();

            foreach (var key in keys)
            {
                string value;
                if (key.Contains('.'))
                {
                    var parts = key.Split('.');
                    value = obj[parts[0]]?[parts[1]]?.ToString()!;
                }
                else
                {
                    value = obj[key]?.ToString()!;
                }

                stringBuilder.Append(value?.ToLower() == "true" ? "true" : value?.ToLower() == "false" ? "false" : value);
            }

            var keyByte = Encoding.UTF8.GetBytes(_paymobSettings.HmacSecret);
            using var hmacSha512 = new HMACSHA512(keyByte);
            var messageBytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());
            var hash = hmacSha512.ComputeHash(messageBytes);

            try
            {
                var receivedHash = Convert.FromHexString(receivedHmac);
                return receivedHash.Length == hash.Length && CryptographicOperations.FixedTimeEquals(hash, receivedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
