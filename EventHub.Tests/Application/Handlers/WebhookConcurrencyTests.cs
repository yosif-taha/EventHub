using EventHub.Application.Common.Dtos.Registrations.Payments;
using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Payments;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public class WebhookConcurrencyTests
{
    [Theory]
    [InlineData(PaymentTransactionStatus.Success, true, "200", true)]
    [InlineData(PaymentTransactionStatus.Failed, false, "200", true)]
    [InlineData(PaymentTransactionStatus.Canceled, false, "200", true)]
    [InlineData(PaymentTransactionStatus.Pending, true, "200", false)]
    [InlineData(PaymentTransactionStatus.Success, true, "201", false)]
    public async Task ConcurrentCallback_AcknowledgesOnlyTheAlreadyPersistedOutcome(PaymentTransactionStatus status, bool success, string transactionId, bool acknowledged)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100);
        var registration = await fixture.SeedRegistrationAsync(entity);
        var payment = await fixture.SeedPaymentAsync(registration, status);
        await fixture.Db.PaymentTransactions.ExecuteUpdateAsync(s => s.SetProperty(p => p.PaymobOrderId, "100").SetProperty(p => p.PaymobTransactionId, transactionId));
        var handler = new ProcessPaymobWebhookCommandHandler(new FailingUnitOfWork(new DbUpdateConcurrencyException()),
            fixture.Repository<Registration>(), fixture.Repository<PaymentTransaction>(), fixture.Repository<Event>(), fixture.Mediator);
        var callback = new PaymobTransactionObj { Id = 200, Success = success, AmountCents = 10000, Currency = "EGP",
            Order = new() { Id = 100, MerchantOrderId = payment.MerchantOrderId } };

        // Act
        var result = await handler.Handle(new ProcessPaymobWebhookCommand(callback), default);

        // Assert
        Assert.Equal(acknowledged, result.IsSuccess);
        if (!acknowledged) Assert.Equal(ErrorCode.InternalServerError, result.ErrorCode);
        Assert.Equal(status, (await fixture.Db.PaymentTransactions.SingleAsync()).Status);
        Assert.Empty(await fixture.Db.Notifications.ToListAsync());
    }
}
