using EventHub.Domin.Common;
using EventHub.Domin.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Domin.Models
{
    public class PaymentTransaction : BaseModel
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public PaymentTransactionStatus Status { get; set; } 
        public string? StripePaymentIntentId { get; set; } 
        public DateTime CreatedAt { get; set; }
        public Guid RegistrationId { get; set; }
        public Registration Registration { get; set; } = null!;
    }
}
