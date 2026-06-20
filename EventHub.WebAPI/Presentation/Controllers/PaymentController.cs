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
        public async Task<IActionResult> HandleWebhook([FromBody] JsonObject rawJsonPayload, [FromQuery] string hmac)
        {
            if (!ValidateHmac(rawJsonPayload, hmac))
                return Unauthorized("Invalid HMAC signature.");

            var payload = rawJsonPayload.Deserialize<PaymobTransactionObj>(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            if (payload == null) 
                return BadRequest();

            var result = await _mediator.Send(new ProcessPaymobWebhookCommand(payload));

            if (!result.IsSuccess)
                return Ok(result);

            return Ok();
        }

        private bool ValidateHmac(JsonObject payload, string receivedHmac)
        {
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

            string computedHmac = BitConverter.ToString(hash).Replace("-", "").ToLower();

            return computedHmac == receivedHmac;
        }
    }
}
