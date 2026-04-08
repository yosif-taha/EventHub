using EventHub.Domin.Common;

namespace EventHub.Domin.Models
{
    public class EventCategory : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public List<Event> Events { get; set; } = []; // Navigation property
    }
}
