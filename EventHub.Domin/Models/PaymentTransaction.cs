using EventHub.Domin.Common;
using EventHub.Domin.Enums;

namespace EventHub.Domin.Models
{
    public class PaymentTransaction : BaseModel
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public PaymentTransactionStatus Status { get; set; }
        public string? PaymobOrderId { get; set; } 
        public string? PaymobTransactionId { get; set; }
        /// <summary>
        /// A stable, locally generated correlation value submitted to Paymob as merchant_order_id.
        /// It remains available when the provider succeeds but recording the provider order ID fails.
        /// </summary>
        public string MerchantOrderId { get; set; } = string.Empty;
        public string? PaymentUrl { get; set; }
        public PaymentOrderCreationStatus OrderCreationStatus { get; set; } = PaymentOrderCreationStatus.Pending;
        public int OrderCreationAttempts { get; set; }
        public DateTime? OrderCreationLastAttemptAt { get; set; }
        public string? OrderCreationFailureReason { get; set; }
        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[] RowVersion { get; set; } = null!;
        public Guid RegistrationId { get; set; }
        public Registration Registration { get; set; } = null!;
    }
}
