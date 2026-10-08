using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Registerations.RegisterationForEvent;
using EventHub.Application.Features.Registerations.CancelRegistrationForEvent;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "Integration")]
public sealed class RegistrationWorkflowTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task Register_ReservesLastSlotAndPersistsAppropriateState(int price)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(price, 9, 10);
        fixture.Payments.Setup(x => x.GeneratePaymentLinkAsync(It.IsAny<PaymobPaymentRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (PaymobPaymentRequest request, CancellationToken _) =>
            {
                using var verification = fixture.CreateContext();
                var intent = await verification.PaymentTransactions.SingleAsync();
                Assert.Equal(request.MerchantOrderId, intent.MerchantOrderId);
                Assert.Equal(PaymentOrderCreationStatus.Processing, intent.OrderCreationStatus);
                return new PaymobPaymentResponse("https://payments.example.test/order", "100");
            });

        // Act
        var result = await fixture.Mediator.Send(new RegisterationCommand(entity.Id));

        // Assert
        Assert.True(result.IsSuccess, result.Message);
        using var saved = fixture.CreateContext();
        Assert.Equal(10, (await saved.Events.SingleAsync()).CurrentAttendeesCount);
        var registration = await saved.Registrations.SingleAsync();
        Assert.Equal(price == 0 ? RegistrationStatus.Confirmed : RegistrationStatus.Pending, registration.Status);
        Assert.Equal(price == 0 ? 1 : 0, await saved.Notifications.CountAsync());
        Assert.Equal(price == 0 ? 0 : 1, await saved.PaymentTransactions.CountAsync());
    }

    [Theory]
    [InlineData(EventStatus.Scheduled, 10, false, ErrorCode.EventIsFull)]
    [InlineData(EventStatus.Completed, 0, false, ErrorCode.RegistrationClosed)]
    [InlineData(EventStatus.Canceled, 0, false, ErrorCode.RegistrationClosed)]
    [InlineData(EventStatus.Scheduled, 0, true, ErrorCode.RegistrationClosed)]
    public async Task Register_RejectsUnavailableEventWithoutMutation(EventStatus status, int count, bool past, ErrorCode error)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(count: count);
        var tracked = await fixture.Db.Events.AsTracking().SingleAsync();
        tracked.Status = status;
        if (past) tracked.EventDate = new DateTime(2000, 1, 1);
        await fixture.Db.SaveChangesAsync();

        // Act
        var result = await fixture.Mediator.Send(new RegisterationCommand(entity.Id));

        // Assert
        Assert.Equal(error, result.ErrorCode);
        Assert.Empty(await fixture.Db.Registrations.ToListAsync());
        Assert.Equal(count, (await fixture.Db.Events.SingleAsync()).CurrentAttendeesCount);
    }

    [Theory]
    [InlineData(RegistrationStatus.Confirmed)]
    [InlineData(RegistrationStatus.Refunded)]
    public async Task Register_RejectsExistingNonCanceledRegistration(RegistrationStatus status)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(count: 1);
        await fixture.SeedRegistrationAsync(entity, status);

        // Act
        var result = await fixture.Mediator.Send(new RegisterationCommand(entity.Id));

        // Assert
        Assert.Equal(ErrorCode.AlreadyRegistered, result.ErrorCode);
        Assert.Equal(1, await fixture.Db.Registrations.CountAsync());
        Assert.Equal(1, (await fixture.Db.Events.SingleAsync()).CurrentAttendeesCount);
    }

    [Fact]
    public async Task Register_ReactivatesCanceledRegistration()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync();
        var previous = await fixture.SeedRegistrationAsync(entity, RegistrationStatus.Canceled);

        // Act
        var result = await fixture.Mediator.Send(new RegisterationCommand(entity.Id));

        // Assert
        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(previous.Id, result.Data!.RegistrationId);
        Assert.Equal(RegistrationStatus.Confirmed, (await fixture.Db.Registrations.SingleAsync()).Status);
        Assert.Single(await fixture.Db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task PaidRegistration_RejectsMissingBillingDetails()
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(100);
        fixture.Accounts.Setup(x => x.GetUserProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<UserProfileResponse>.Success(new("a@example.test", "a", "A", null)));

        // Act
        var result = await fixture.Mediator.Send(new RegisterationCommand(entity.Id));

        // Assert
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.Empty(await fixture.Db.Registrations.ToListAsync());
        fixture.Payments.Verify(x => x.GeneratePaymentLinkAsync(It.IsAny<PaymobPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(RegistrationStatus.Pending)]
    [InlineData(RegistrationStatus.Confirmed)]
    public async Task Cancel_ReleasesCapacityAndCancelsPendingPayment_OnlyOnce(RegistrationStatus status)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(100, 1);
        var registration = await fixture.SeedRegistrationAsync(entity, status);
        await fixture.SeedPaymentAsync(registration);

        // Act
        var first = await fixture.Mediator.Send(new CancelRegistrationCommand(registration.Id));
        var second = await fixture.Mediator.Send(new CancelRegistrationCommand(registration.Id));

        // Assert
        Assert.True(first.IsSuccess);
        Assert.Equal(ErrorCode.RegistrationAlreadyCanceled, second.ErrorCode);
        Assert.Equal(0, (await fixture.Db.Events.SingleAsync()).CurrentAttendeesCount);
        Assert.Equal(PaymentTransactionStatus.Canceled, (await fixture.Db.PaymentTransactions.SingleAsync()).Status);
    }

    [Theory]
    [InlineData("wrong-owner", ErrorCode.UnAuthorized)]
    [InlineData("refunded", ErrorCode.RegistrationClosed)]
    [InlineData("completed", ErrorCode.RegistrationClosed)]
    [InlineData("past", ErrorCode.RegistrationClosed)]
    [InlineData("wrong-role", ErrorCode.Forbidden)]
    public async Task Cancel_RejectsIneligibleRequestWithoutChangingCapacity(string scenario, ErrorCode error)
    {
        // Arrange
        using var fixture = new WorkflowFixture();
        var entity = await fixture.SeedEventAsync(count: 1);
        var registration = await fixture.SeedRegistrationAsync(entity,
            scenario == "refunded" ? RegistrationStatus.Refunded : RegistrationStatus.Confirmed);
        if (scenario == "wrong-owner") fixture.User.UserId = WorkflowFixture.OtherId;
        if (scenario == "wrong-role") fixture.User.Role = RoleNames.Organizer;
        var tracked = await fixture.Db.Events.AsTracking().SingleAsync();
        if (scenario == "completed") tracked.Status = EventStatus.Completed;
        if (scenario == "past") tracked.EventDate = new DateTime(2000, 1, 1);
        await fixture.Db.SaveChangesAsync();

        // Act
        var result = await fixture.Mediator.Send(new CancelRegistrationCommand(registration.Id));

        // Assert
        Assert.Equal(error, result.ErrorCode);
        Assert.Equal(1, (await fixture.Db.Events.SingleAsync()).CurrentAttendeesCount);
    }

    [Fact]
    public async Task MissingRecordsAndWrongRole_ReturnExpectedErrors()
    {
        // Arrange
        using var fixture = new WorkflowFixture();

        // Act
        var missingEvent = await fixture.Mediator.Send(new RegisterationCommand(WorkflowFixture.OtherId));
        var missingRegistration = await fixture.Mediator.Send(new CancelRegistrationCommand(WorkflowFixture.OtherId));
        fixture.User.Role = RoleNames.Organizer;
        var forbidden = await fixture.Mediator.Send(new RegisterationCommand(WorkflowFixture.OtherId));

        // Assert
        Assert.Equal(ErrorCode.EventNotFound, missingEvent.ErrorCode);
        Assert.Equal(ErrorCode.RegistrationNotFound, missingRegistration.ErrorCode);
        Assert.Equal(ErrorCode.Forbidden, forbidden.ErrorCode);
    }
}
