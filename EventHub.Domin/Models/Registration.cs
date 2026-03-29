using EventHub.Domin.Common;
using EventHub.Domin.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Domin.Models
{
    public class Registration : BaseModel
    {
        public DateTime RegistrationDate { get; set; }
        public PaymentStatus Status { get; set; }
        public Guid UserId { get; set; } = Guid.CreateVersion7(); // Foreign key to ApplicationUser
        public ApplicationUser User { get; set; } = null!; // Navigation property 
        public Guid EventId { get; set; } = Guid.CreateVersion7(); // Foreign key to Event
        public Event Event { get; set; } = null!; // Navigation property
    }
}
