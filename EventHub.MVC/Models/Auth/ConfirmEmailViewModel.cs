using System.ComponentModel.DataAnnotations;

namespace EventHub.MVC.Models.Auth;

public sealed class ConfirmEmailViewModel
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;
}
