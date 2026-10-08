using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Registerations.RegisterationForEvent;
using EventHub.Application.Features.Registerations.CancelRegistrationForEvent;
using EventHub.Application.Features.Payments.CreatePaymobPaymentOrder;
using EventHub.Domin.Models;
using EventHub.Tests.Support;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Moq;
using Xunit;

namespace EventHub.Tests.Application.Handlers;

[Trait("Category", "MockUnit")]
public class WorkflowFailureTests
{
    [Theory]
    [InlineData(true, ErrorCode.ConcurrencyConflict)]
    [InlineData(false, ErrorCode.InternalServerError)]
    public async Task RegistrationAndCancellation_TranslateTransactionFailureWithoutCallingDependencies(bool concurrency, ErrorCode expected)
    {
        // Arrange
        var unit = new FailingUnitOfWork(concurrency ? new DbUpdateConcurrencyException("stale") : new InvalidOperationException("private details"));
        var events = new Mock<IGenericRepository<Event>>(MockBehavior.Strict);
        var payments = new Mock<IGenericRepository<PaymentTransaction>>(MockBehavior.Strict);
        var registrations = new Mock<IGenericRepository<Registration>>(MockBehavior.Strict);
        var account = new Mock<IAccountService>(MockBehavior.Strict);
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var user = new TestUserContext();
        var register = new RegisterForEventCommandHandler(unit, user, events.Object, payments.Object, registrations.Object, account.Object, mediator.Object);
        var cancel = new CancelRegistrationCommandHandler(unit, user, registrations.Object, events.Object, payments.Object);

        // Act
        var registration = await register.Handle(new RegisterationCommand(WorkflowFixture.OtherId), default);
        var cancellation = await cancel.Handle(new CancelRegistrationCommand(WorkflowFixture.OtherId), default);

        // Assert
        Assert.Equal(expected, registration.ErrorCode); Assert.Equal(expected, cancellation.ErrorCode);
        Assert.DoesNotContain("private details", registration.Message ?? "");
        events.VerifyNoOtherCalls(); payments.VerifyNoOtherCalls(); registrations.VerifyNoOtherCalls(); account.VerifyNoOtherCalls(); mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PaymentClaimDatabaseFailure_ReturnsDatabaseErrorWithoutContactingProvider()
    {
        // Arrange
        var payments = new Mock<IGenericRepository<PaymentTransaction>>(MockBehavior.Strict);
        var provider = new Mock<IPaymobService>(MockBehavior.Strict);
        var accounts = new Mock<IAccountService>(MockBehavior.Strict);
        var handler = new CreatePaymobPaymentOrderCommandHandler(new FailingUnitOfWork(new DbUpdateException("private database details")),
            payments.Object, accounts.Object, new TestUserContext(), provider.Object);

        // Act
        var result = await handler.Handle(new CreatePaymobPaymentOrderCommand(WorkflowFixture.OtherId), default);

        // Assert
        Assert.Equal(ErrorCode.DatabaseError, result.ErrorCode);
        payments.VerifyNoOtherCalls(); accounts.VerifyNoOtherCalls(); provider.VerifyNoOtherCalls();
    }
}
