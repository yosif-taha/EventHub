using EventHub.Application.Contracts;
using EventHub.Infrastructure.Payment;
using EventHub.Tests.Support;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Xunit;

namespace EventHub.Tests.Infrastructure;

[Trait("Category", "Unit")]
public class PaymobServiceTests
{
    [Theory]
    [InlineData("10.005", 1001)]
    [InlineData("10.004", 1000)]
    public async Task GenerateLink_SendsStableMerchantReferenceBillingAndRoundedAmount(string amount, int cents)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(i => RecordingHttpHandler.Json(i switch {
            0 => "{\"token\":\"auth-token\"}", 1 => "{\"id\":123}", _ => "{\"token\":\"payment-token\"}"
        }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://provider.example.test/api/") };
        var service = new PaymobService(client, Options.Create(Settings()));
        var request = Request(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        var result = await service.GeneratePaymentLinkAsync(request, default);

        // Assert
        Assert.Equal("123", result.PaymobOrderId);
        Assert.Equal("https://accept.paymob.com/api/acceptance/iframes/456?payment_token=payment-token", result.PaymentUrl);
        Assert.Equal(3, handler.Requests.Count);
        Assert.EndsWith("/auth/tokens", handler.Requests[0].Uri);
        Assert.EndsWith("/ecommerce/orders", handler.Requests[1].Uri);
        Assert.EndsWith("/acceptance/payment_keys", handler.Requests[2].Uri);
        Assert.All(handler.Requests, r => Assert.Equal(HttpMethod.Post, r.Method));
        using var order = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.Equal(cents, order.RootElement.GetProperty("amount_cents").GetInt32());
        Assert.Equal("merchant-reference", order.RootElement.GetProperty("merchant_order_id").GetString());
        using var key = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.Equal("https://app.example.test/return/" + request.RegistrationId, key.RootElement.GetProperty("redirection_url").GetString());
        Assert.Equal("attendee@example.test", key.RootElement.GetProperty("billing_data").GetProperty("email").GetString());
        Assert.Equal(cents, key.RootElement.GetProperty("amount_cents").GetInt32());
    }

    [Theory]
    [InlineData("")]
    [InlineData("/relative")]
    [InlineData("http://app.example.test/return")]
    public async Task InvalidReturnUrl_FailsBeforeSendingAnyRequest(string url)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(_ => throw new InvalidOperationException("Unexpected network call"));
        using var client = new HttpClient(handler);
        var settings = Settings(); settings.ReturnUrl = url;
        var service = new PaymobService(client, Options.Create(settings));

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GeneratePaymentLinkAsync(Request(10), default));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ProviderFailure_StopsAtFailedStage(int failedStage)
    {
        // Arrange
        using var handler = new RecordingHttpHandler(i => i == failedStage
            ? RecordingHttpHandler.Json("{}", HttpStatusCode.BadGateway)
            : RecordingHttpHandler.Json(i == 0 ? "{\"token\":\"auth\"}" : "{\"id\":123}"));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://provider.example.test/") };
        var service = new PaymobService(client, Options.Create(Settings()));

        // Act / Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GeneratePaymentLinkAsync(Request(10), default));
        Assert.Equal(failedStage + 1, handler.Requests.Count);
    }

    private static PaymobSettings Settings() => new() {
        ApiKey = "test-key", IframeId = "456", CardIntegrationId = "789",
        ReturnUrl = "https://app.example.test/return/{registrationId}"
    };
    private static PaymobPaymentRequest Request(decimal amount) =>
        new(WorkflowFixture.AttendeeId, amount, "Test", "Attendee", "+201000000000", "attendee@example.test", "merchant-reference");
}
