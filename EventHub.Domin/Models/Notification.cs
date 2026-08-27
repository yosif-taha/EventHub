using EventHub.Domin.Common;

namespace EventHub.Domin.Models
{
    public class Notification : BaseModel
    {
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime NotificationDate { get; set; }
        public EventHub.Domin.Enums.NotificationType Type { get; set; }
        public EventHub.Domin.Enums.NotificationDeliveryStatus DeliveryStatus { get; set; } = EventHub.Domin.Enums.NotificationDeliveryStatus.Pending;
        public DateTime? SentAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public Guid? DeliveryLeaseId { get; set; }
        public DateTime? DeliveryLeaseExpiresAt { get; set; }
        public int DeliveryAttempts { get; set; }
        public string? DeduplicationKey { get; set; }
        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[] RowVersion { get; set; } = null!;
        public Guid UserId { get; set; } // Foreign key to ApplicationUser
        public ApplicationUser User { get; set; } = null!; // Navigation property 
        public Guid EventId { get; set; }  // Foreign key to Event
        public Event Event { get; set; } = null!; // Navigation property

    }
}
