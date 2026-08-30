using System.ComponentModel.DataAnnotations;

namespace EventHub.Persistence.DataSeeding;

public sealed class BootstrapAdminSettings : IValidatableObject
{
    public bool Enabled { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? FullName { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
            yield break;

        if (string.IsNullOrWhiteSpace(Email) || !new EmailAddressAttribute().IsValid(Email))
            yield return new ValidationResult("BootstrapAdmin:Email must be a valid email address when bootstrap is enabled.", [nameof(Email)]);

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
            yield return new ValidationResult("BootstrapAdmin:Password must contain at least eight characters when bootstrap is enabled.", [nameof(Password)]);

        if (string.IsNullOrWhiteSpace(FullName))
            yield return new ValidationResult("BootstrapAdmin:FullName is required when bootstrap is enabled.", [nameof(FullName)]);
    }
}
