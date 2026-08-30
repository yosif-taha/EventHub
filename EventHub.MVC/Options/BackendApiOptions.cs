using System.ComponentModel.DataAnnotations;

namespace EventHub.MVC.Options;

public sealed class BackendApiOptions
{
    public const string SectionName = "BackendApi";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = "https://localhost:7158/";
}
