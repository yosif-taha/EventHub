using EventHub.Domin.Enums;

namespace EventHub.Application.Common.Dtos.Events
{
    public class EventDto
    {
        public Guid Id { get; set; } 
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int MaxAttendees { get; set; }
        public int CurrentAttendeesCount { get; set; }
        public int RemainingSlots { get; set; }
        public string Status { get; set; } = string.Empty; 
        public bool PaymentRequired { get; set; }
        public decimal Price { get; set; }
        public EventMode Mode { get; set; }
        public string? OnlineMeetingUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
