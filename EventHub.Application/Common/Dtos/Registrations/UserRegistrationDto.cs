
namespace EventHub.Application.Common.Dtos.Registrations
{
    public class UserRegistrationDto
    {
        public Guid Id { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Status { get; set; } = null!;
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = null!;
        public DateTime EventStartDate { get; set; }
        public string EventLocation { get; set; } = null!;
    }
}
