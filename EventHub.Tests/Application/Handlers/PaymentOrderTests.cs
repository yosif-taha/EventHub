using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Payments.CreatePaymobPaymentOrder;
using EventHub.Domin.Enums;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public class PaymentOrderTests
{
    [Theory]
    [InlineData(PaymentOrderCreationStatus.Pending, true)]
    [InlineData(PaymentOrderCreationStatus.Failed, true)]
    [InlineData(PaymentOrderCreationStatus.Processing, false)]
    [InlineData(PaymentOrderCreationStatus.Created, true)]
    public async Task OrderCreation_RespectsDurableClaimAndReusesExistingLink(PaymentOrderCreationStatus state, bool success)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100, count: 1);
        var registration = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Pending);
        await fixture.SeedPaymentAsync(registration, orderStatus: state);
        if (state == PaymentOrderCreationStatus.Created)
            await fixture.Db.PaymentTransactions.ExecuteUpdateAsync(s => s.SetProperty(p => p.PaymentUrl, "https://existing.example.test/order"));

        // Act
        var result = await fixture.Mediator.Send(new CreatePaymobPaymentOrderCommand(registration.Id));

        // Assert
        Assert.Equal(success, result.IsSuccess);
        var calls = state is PaymentOrderCreationStatus.Pending or PaymentOrderCreationStatus.Failed ? 1 : 0;
        fixture.Payments.Verify(p => p.GeneratePaymentLinkAsync(It.Is<PaymobPaymentRequest>(r =>
            r.Amount == 100 && r.RegistrationId == registration.Id && r.MerchantOrderId == registration.Id.ToString()), It.IsAny<CancellationToken>()), Times.Exactly(calls));
        var stored = await fixture.Db.PaymentTransactions.SingleAsync();
        Assert.Equal(calls, stored.OrderCreationAttempts);
        if (success) Assert.Equal(PaymentOrderCreationStatus.Created, stored.OrderCreationStatus);
        if (state == PaymentOrderCreationStatus.Created) Assert.Equal("https://existing.example.test/order", result.Data!.PaymentUrl);
        Assert.Equal(1, (await fixture.Db.Events.SingleAsync()).CurrentAttendeesCount);
    }

    [Fact]
    public async Task ProviderFailure_PersistsFailedAttemptAndAllowsExplicitRetry()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100, count: 1);
        var registration = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Pending);
        await fixture.SeedPaymentAsync(registration);
        fixture.Payments.SetupSequence(p => p.GeneratePaymentLinkAsync(It.IsAny<PaymobPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("simulated"))
            .ReturnsAsync(new PaymobPaymentResponse("https://provider.example.test/order", "100"));

        // Act
        var failure = await fixture.Mediator.Send(new CreatePaymobPaymentOrderCommand(registration.Id));
        var failedState = await fixture.Db.PaymentTransactions.SingleAsync();
        var retry = await fixture.Mediator.Send(new CreatePaymobPaymentOrderCommand(registration.Id));

        // Assert
        Assert.Equal(ErrorCode.PaymentProviderError, failure.ErrorCode);
        Assert.Equal(PaymentOrderCreationStatus.Failed, failedState.OrderCreationStatus);
        Assert.NotNull(failedState.OrderCreationFailureReason);
        Assert.True(retry.IsSuccess);
        Assert.Equal(2, (await fixture.Db.PaymentTransactions.SingleAsync()).OrderCreationAttempts);
        Assert.Equal(RegistrationStatus.Pending, (await fixture.Db.Registrations.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(RegistrationStatus.Confirmed, PaymentTransactionStatus.Pending)]
    [InlineData(RegistrationStatus.Canceled, PaymentTransactionStatus.Pending)]
    [InlineData(RegistrationStatus.Pending, PaymentTransactionStatus.Success)]
    [InlineData(RegistrationStatus.Pending, PaymentTransactionStatus.Canceled)]
    public async Task IneligibleState_DoesNotContactProvider(RegistrationStatus registrationStatus, PaymentTransactionStatus paymentStatus)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price: 100);
        var registration = await fixture.SeedRegistrationAsync(entity, registrationStatus);
        await fixture.SeedPaymentAsync(registration, paymentStatus);

        // Act
        var result = await fixture.Mediator.Send(new CreatePaymobPaymentOrderCommand(registration.Id));

        // Assert
        Assert.Equal(ErrorCode.PaymentProviderError, result.ErrorCode);
        fixture.Payments.VerifyNoOtherCalls();
        Assert.Equal(0, (await fixture.Db.PaymentTransactions.SingleAsync()).OrderCreationAttempts);
    }
}
