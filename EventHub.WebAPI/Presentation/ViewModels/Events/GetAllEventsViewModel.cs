namespace EventHub.WebAPI.Presentation.ViewModels.Events
{
    public class GetAllEventsViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int MaxAttendees { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool PaymentRequired { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
