
using System.ComponentModel.DataAnnotations;

namespace EventHub.Infrastructure.Payment
{
    public class PaymobSettings : IValidatableObject
    {
        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string HmacSecret { get; set; } = string.Empty;

        [Required]
        public string CardIntegrationId { get; set; } = string.Empty;

        [Required]
        public string IframeId { get; set; } = string.Empty;

        [Required]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string ReturnUrl { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                yield return new ValidationResult(
                    "Paymob BaseUrl must be an absolute HTTP or HTTPS URL.",
                    [nameof(BaseUrl)]);
            }

            var returnUrl = ReturnUrl.Replace("{registrationId}", "00000000-0000-0000-0000-000000000000", StringComparison.Ordinal);
            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var parsedReturnUrl) ||
                (parsedReturnUrl.Scheme != Uri.UriSchemeHttp && parsedReturnUrl.Scheme != Uri.UriSchemeHttps))
            {
                yield return new ValidationResult(
                    "Paymob ReturnUrl must be an absolute HTTP or HTTPS URL and may contain the {registrationId} placeholder.",
                    [nameof(ReturnUrl)]);
            }
        }
    }
}
