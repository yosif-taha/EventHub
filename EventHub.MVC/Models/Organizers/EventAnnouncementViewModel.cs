using System.ComponentModel.DataAnnotations;

namespace EventHub.MVC.Models.Organizers;

public sealed class EventAnnouncementViewModel
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public int RegistrationCount { get; set; }

    [Required]
    [StringLength(250)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = string.Empty;
}
