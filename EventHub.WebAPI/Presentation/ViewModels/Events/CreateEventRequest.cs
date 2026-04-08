namespace EventHub.WebAPI.Presentation.ViewModels.Events
{
    public class CreateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; } 
        public Guid CategoryId { get; set; } 
        public int MaxAttendees { get; set; }
    }
}
