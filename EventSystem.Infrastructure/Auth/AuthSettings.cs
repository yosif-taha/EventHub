using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EventHub.Infrastructure.Auth;

public sealed class AuthSettings
{
    public string PublicBaseUrl { get; init; } = string.Empty;
}

public sealed class AuthSettingsValidator(IHostEnvironment environment) : IValidateOptions<AuthSettings>
{
    public ValidateOptionsResult Validate(string? name, AuthSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl) ||
            !Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var publicBaseUri))
        {
            return ValidateOptionsResult.Fail("AuthSettings:PublicBaseUrl must be an absolute URL.");
        }

        if (publicBaseUri.Scheme != Uri.UriSchemeHttp && publicBaseUri.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail("AuthSettings:PublicBaseUrl must use HTTP or HTTPS.");
        }

        if (!environment.IsDevelopment() && publicBaseUri.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail("AuthSettings:PublicBaseUrl must use HTTPS outside the Development environment.");
        }

        if (!string.IsNullOrEmpty(publicBaseUri.UserInfo) ||
            !string.IsNullOrEmpty(publicBaseUri.Query) ||
            !string.IsNullOrEmpty(publicBaseUri.Fragment) ||
            !publicBaseUri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail(
                "AuthSettings:PublicBaseUrl must not include user information, query text, or a fragment and must end with '/'.");
        }

        return ValidateOptionsResult.Success;
    }
}
