using EventHub.Domin.Common;
using EventHub.Domin.Enums;
using System.ComponentModel.DataAnnotations;

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
        public int CurrentAttendeesCount { get; set; }
        public int RemainingSlots { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsCancelled { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
        public Guid OrganizerId { get; set; } // Foreign key to ApplicationUser
        public ApplicationUser Organizer { get; set; } = null!; // Navigation property
        public Guid? CategoryId { get; set; }  // Foreign key to Category
        public EventCategory Category { get; set; } = null!; // Navigation property
        public List<Registration> Registrations { get; set; } = []; // Navigation property
        public List<PaymentTransaction> PaymentTransactions { get; set; } = []; // Navigation property
        public List<Notification> Notifications { get; set; } = []; // Navigation property

        public bool TryIncrementAttendees()
        {
            if (CurrentAttendeesCount >= MaxAttendees)
                return false;

            CurrentAttendeesCount++;
            return true;
        }
        public void DecrementAttendees()
        {
            if (CurrentAttendeesCount > 0)
            {
                CurrentAttendeesCount--;
            }
        }
    }
}
