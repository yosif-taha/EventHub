namespace EventHub.Application.Common.Dtos.Registrations
{
    public class EventRegistrationDto
    {
        public Guid RegistrationId { get; set; }
        public Guid AttendeeId { get; set; }
        public string AttendeeName { get; set; } = string.Empty;
        public string AttendeeEmail { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
