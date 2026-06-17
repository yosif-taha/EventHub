using EventHub.Domin.Common;
using EventHub.Domin.Enums;

namespace EventHub.Domin.Models
{
    public class Registration : BaseModel
    {
        public DateTime RegistrationDate { get; set; }
        public RegistrationStatus Status { get; set; }
        public Guid UserId { get; set; } 
        public Guid EventId { get; set; } // Foreign key to Event
        public Event Event { get; set; } = null!; // Navigation property
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}
