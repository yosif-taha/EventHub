using EventHub.Domin.Enums;
using EventHub.MVC.Models.Events;
using System.ComponentModel.DataAnnotations;

namespace EventHub.MVC.Models.Organizers;

public sealed class EventEditorViewModel : IValidatableObject
{
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Date and time (UTC)")]
    [DataType(DataType.DateTime)]
    public DateTime EventDate { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "Category")]
    public Guid? CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Maximum attendees")]
    public int MaxAttendees { get; set; }

    [Display(Name = "Event mode")]
    public EventMode Mode { get; set; }

    [Display(Name = "Online meeting URL")]
    [Url]
    public string? OnlineMeetingUrl { get; set; }

    [Range(0, double.MaxValue)]
    public double Price { get; set; }

    public bool IsEdit { get; set; }
    public string CurrentCategoryName { get; set; } = string.Empty;
    public IReadOnlyList<CategoryDto> Categories { get; set; } = [];
    public string? CategoryLoadError { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateTime.SpecifyKind(EventDate, DateTimeKind.Utc) <= DateTime.UtcNow)
            yield return new ValidationResult("Event date must be in the future.", [nameof(EventDate)]);

        if (Mode == EventMode.Online && string.IsNullOrWhiteSpace(OnlineMeetingUrl))
            yield return new ValidationResult("An absolute online meeting URL is required for online events.", [nameof(OnlineMeetingUrl)]);

        if (Mode == EventMode.Offline && !string.IsNullOrWhiteSpace(OnlineMeetingUrl))
            yield return new ValidationResult("Offline events cannot include an online meeting URL.", [nameof(OnlineMeetingUrl)]);
    }
}
