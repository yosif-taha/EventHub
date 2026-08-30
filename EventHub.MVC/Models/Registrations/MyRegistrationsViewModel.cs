using EventHub.MVC.Models.Common;

namespace EventHub.MVC.Models.Registrations;

public sealed class MyRegistrationsViewModel
{
    public PaginatedResult<UserRegistrationDto> Registrations { get; init; } = new();
    public string? ErrorMessage { get; init; }
}
