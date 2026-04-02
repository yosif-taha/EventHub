using EventHub.Domin.Common;
using EventHub.Domin.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Domin.Models
{
    public class Event : BaseModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; } 
        public int MaxAttendees { get; set; }
        public EventStatus Status { get; set; }
        public bool PaymentRequired { get; set; }
        public Guid OrganizerId { get; set; } = Guid.CreateVersion7(); // Foreign key to ApplicationUser
        public ApplicationUser Organizer { get; set; } = null!; // Navigation property
        public Guid? CategoryId { get; set; } = Guid.CreateVersion7();  // Foreign key to Category
        public EventCategory Category { get; set; } = null!; // Navigation property
        public List<Registration> Registrations { get; set; } = []; // Navigation property
        public List<PaymentTransaction> PaymentTransactions { get; set; } = []; // Navigation property
        public List<Notification> Notifications { get; set; } = []; // Navigation property
    }
}
