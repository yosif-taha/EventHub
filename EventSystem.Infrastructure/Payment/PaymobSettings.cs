
namespace EventHub.Infrastructure.Payment
{
    public class PaymobSettings
    {
        public string ApiKey { get; set; } = null!;
        public string HmacSecret { get; set; } = null!;
        public string CardIntegrationId { get; set; } = null!;
        public string IframeId { get; set; } = null!;
        public string BaseUrl { get; set; } = null!;
    }
}
