using EventHub.Application.Common.Dtos.Registrations.Payments;
using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Payments;
using EventHub.Domin.Enums;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public class PaymentWebhookTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FinalCallback_PersistsOutcomeAndRepeatedDeliveryHasNoAdditionalEffects(bool success)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100, count: 1);
        var registration = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Pending);
        var payment = await fixture.SeedPaymentAsync(registration);
        var callback = Callback(payment.MerchantOrderId, success);

        // Act
        var first = await fixture.Mediator.Send(new ProcessPaymobWebhookCommand(callback));
        var repeated = await fixture.Mediator.Send(new ProcessPaymobWebhookCommand(callback));

        // Assert
        Assert.True(first.IsSuccess);
        Assert.True(repeated.IsSuccess);
        var stored = await fixture.Db.PaymentTransactions.SingleAsync();
        Assert.Equal(success ? PaymentTransactionStatus.Success : PaymentTransactionStatus.Failed, stored.Status);
        Assert.Equal("100", stored.PaymobOrderId);
        Assert.Equal("200", stored.PaymobTransactionId);
        Assert.Equal(PaymentOrderCreationStatus.Created, stored.OrderCreationStatus);
        Assert.Equal(success ? RegistrationStatus.Confirmed : RegistrationStatus.Canceled, (await fixture.Db.Registrations.SingleAsync()).Status);
        Assert.Equal(success ? 1 : 0, (await fixture.Db.Events.SingleAsync()).CurrentAttendeesCount);
        Assert.Equal(success ? 1 : 0, await fixture.Db.Notifications.CountAsync());
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("currency")]
    [InlineData("merchant")]
    [InlineData("order")]
    [InlineData("transaction")]
    public async Task InvalidCallback_DoesNotChangePaymentOrRegistration(string invalidField)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100, count: 1);
        var registration = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Pending);
        var payment = await fixture.SeedPaymentAsync(registration);
        await fixture.Db.PaymentTransactions.ExecuteUpdateAsync(s => s.SetProperty(p => p.PaymobOrderId, "100"));
        var callback = Callback(payment.MerchantOrderId, true);
        switch (invalidField)
        {
            case "amount": callback.AmountCents++; break;
            case "currency": callback.Currency = "USD"; break;
            case "merchant": callback.Order.MerchantOrderId = "wrong"; break;
            case "order": callback.Order = null!; break;
            case "transaction": callback.Id = 0; break;
        }

        // Act
        var result = await fixture.Mediator.Send(new ProcessPaymobWebhookCommand(callback));

        // Assert
        Assert.Equal(ErrorCode.PaymentProviderError, result.ErrorCode);
        Assert.Equal(PaymentTransactionStatus.Pending, (await fixture.Db.PaymentTransactions.SingleAsync()).Status);
        Assert.Equal(RegistrationStatus.Pending, (await fixture.Db.Registrations.SingleAsync()).Status);
        Assert.Equal(1, (await fixture.Db.Events.SingleAsync()).CurrentAttendeesCount);
        Assert.Empty(await fixture.Db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task PendingCallback_ReconcilesOrderWithoutConfirmingRegistration()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100, count: 1);
        var registration = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Pending);
        var payment = await fixture.SeedPaymentAsync(registration, orderStatus: PaymentOrderCreationStatus.Processing);
        var callback = Callback(payment.MerchantOrderId, true);
        callback.Pending = true;

        // Act
        var result = await fixture.Mediator.Send(new ProcessPaymobWebhookCommand(callback));

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await fixture.Db.PaymentTransactions.SingleAsync();
        Assert.Equal("100", stored.PaymobOrderId);
        Assert.Equal(PaymentTransactionStatus.Pending, stored.Status);
        Assert.Null(stored.PaymobTransactionId);
        Assert.Equal(RegistrationStatus.Pending, (await fixture.Db.Registrations.SingleAsync()).Status);
        Assert.Empty(await fixture.Db.Notifications.ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanceledPayment_RecordsLateCallbackWithoutResurrectingRegistration(bool success)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100);
        var registration = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Canceled);
        var payment = await fixture.SeedPaymentAsync(registration, PaymentTransactionStatus.Canceled);

        // Act
        var result = await fixture.Mediator.Send(new ProcessPaymobWebhookCommand(Callback(payment.MerchantOrderId, success)));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(success ? PaymentTransactionStatus.Success : PaymentTransactionStatus.Canceled, (await fixture.Db.PaymentTransactions.SingleAsync()).Status);
        Assert.Equal(RegistrationStatus.Canceled, (await fixture.Db.Registrations.SingleAsync()).Status);
        Assert.Equal(0, (await fixture.Db.Events.SingleAsync()).CurrentAttendeesCount);
        Assert.Empty(await fixture.Db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task ConflictingOutcome_IsRejectedAfterSuccessfulCallback()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100, count: 1);
        var registration = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Pending);
        var payment = await fixture.SeedPaymentAsync(registration);
        await fixture.Mediator.Send(new ProcessPaymobWebhookCommand(Callback(payment.MerchantOrderId, true)));

        // Act
        var result = await fixture.Mediator.Send(new ProcessPaymobWebhookCommand(Callback(payment.MerchantOrderId, false)));

        // Assert
        Assert.Equal(ErrorCode.PaymentProviderError, result.ErrorCode);
        Assert.Equal(PaymentTransactionStatus.Success, (await fixture.Db.PaymentTransactions.SingleAsync()).Status);
        Assert.Equal(RegistrationStatus.Confirmed, (await fixture.Db.Registrations.SingleAsync()).Status);
        Assert.Equal(1, await fixture.Db.Notifications.CountAsync());
    }

    [Fact]
    public async Task UnknownPayment_ReturnsTransactionNotFound()
    {
        // Arrange
        using var fixture = new WorkflowFixture();

        // Act
        var result = await fixture.Mediator.Send(new ProcessPaymobWebhookCommand(Callback("unknown", true)));

        // Assert
        Assert.Equal(ErrorCode.TransactionNotFound, result.ErrorCode);
    }

    private static PaymobTransactionObj Callback(string merchantId, bool success) => new()
    {
        Id = 200, AmountCents = 10000, Currency = "egp", Success = success,
        Order = new() { Id = 100, MerchantOrderId = merchantId }
    };
}
