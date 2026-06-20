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
        public Guid RegistrationId { get; set; }
        public Registration Registration { get; set; } = null!;
    }
}
