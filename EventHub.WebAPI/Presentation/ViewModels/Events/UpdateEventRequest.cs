namespace EventHub.WebAPI.Presentation.ViewModels.Events
{
    public class UpdateEventRequest
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public DateTime EventDate { get; set; }
        public int MaxAttendees { get; set; }
    }
}
