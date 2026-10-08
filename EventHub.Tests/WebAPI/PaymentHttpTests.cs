using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Payments;
using EventHub.Tests.Support;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace EventHub.Tests.WebAPI;

[Trait("Category", "Http")]
public class PaymentHttpTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("not-hex")]
    [InlineData("00")]
    public async Task InvalidSignature_IsUnauthorizedAndNeverProcessesPayment(string? signature)
    {
        // Arrange
        using var fixture = new ApiFixture();

        // Act
        var response = await fixture.Client.PostAsJsonAsync("/api/payments/paymob/webhook?hmac=" + signature, Payload());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        fixture.Mediator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.OK)]
    [InlineData(ErrorCode.PaymentProviderError, HttpStatusCode.BadRequest)]
    [InlineData(ErrorCode.TransactionNotFound, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ErrorCode.InternalServerError, HttpStatusCode.ServiceUnavailable)]
    public async Task SignedCallback_MapsProcessingOutcomeToProviderRetryStatus(ErrorCode? error, HttpStatusCode expected)
    {
        // Arrange: canonical string independently specified for the fixed payload.
        using var fixture = new ApiFixture();
        var signature = Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(ApiFixture.HmacSecret), Encoding.UTF8.GetBytes("10000EGP200100falsetrue")));
        fixture.Mediator.Setup(m => m.Send(It.Is<ProcessPaymobWebhookCommand>(c =>
            c.Payload.Id == 200 && c.Payload.Order.Id == 100 && c.Payload.AmountCents == 10000 && c.Payload.Success), It.IsAny<CancellationToken>()))
            .ReturnsAsync(error.HasValue ? RequestResult<bool>.Failure(error.Value) : RequestResult<bool>.Success(true));

        // Act
        var response = await fixture.Client.PostAsJsonAsync("/api/payments/paymob/webhook?hmac=" + signature, Payload());

        // Assert
        Assert.Equal(expected, response.StatusCode);
        fixture.Mediator.VerifyAll();
    }

    [Fact]
    public async Task MissingSecret_ReturnsServiceUnavailable()
    {
        // Arrange
        using var fixture = new ApiFixture("");

        // Act
        var response = await fixture.Client.PostAsJsonAsync("/api/payments/paymob/webhook", Payload());

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        fixture.Mediator.VerifyNoOtherCalls();
    }

    private static JsonObject Payload() => JsonNode.Parse("""
        {"obj":{"amount_cents":10000,"currency":"EGP","id":200,"order":{"id":100,"merchant_order_id":"merchant"},"pending":false,"success":true}}
        """)!.AsObject();
}
