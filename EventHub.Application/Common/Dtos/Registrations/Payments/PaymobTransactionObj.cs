
namespace EventHub.Application.Common.Dtos.Registrations.Payments
{
    public class PaymobTransactionObj
    {
        public long Id { get; set; } 
        public bool Success { get; set; } 
        [System.Text.Json.Serialization.JsonPropertyName("amount_cents")]
        public long AmountCents { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool Pending { get; set; }

        public PaymobOrderDetails Order { get; set; } = null!;
    }
}
