using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Dtos.Registrations.Payments;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Notifications.QueueRegistrationConfirmation;
using EventHub.Application.Features.Payments;
using EventHub.Application.Features.Payments.CreatePaymobPaymentOrder;
using EventHub.Application.Features.Registerations.RegisterationForEvent;
using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using EventHub.Persistence.Reposetories;
using EventHub.Persistence.Unit_Of_Work;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Data;

namespace EventHub.Tests;

public sealed class PaymentIntentTests
{
    [Fact]
    public async Task Paid_registration_persists_a_local_payment_intent_before_provider_dispatch()
    {
        await using var database = new TestDatabase();
        await using var context = database.CreateContext();
        var attendee = new ApplicationUser
        {
            Email = "attendee@example.test",
            UserName = "attendee@example.test",
            FullName = "Test Attendee",
            PhoneNumber = "+201000000000",
            EmailConfirmed = true
        };
        var @event = new Event
        {
            Title = "Paid event",
            Description = "Test event",
            Location = "Cairo",
            EventDate = DateTime.UtcNow.AddDays(2),
            MaxAttendees = 10,
            Price = 100m,
            Status = EventStatus.Scheduled,
            OrganizerId = attendee.Id,
            CreatedAt = DateTime.UtcNow,
            RowVersion = [0]
        };
        context.AddRange(attendee, @event);
        await context.SaveChangesAsync();

        var mediator = new Mock<IMediator>();
        mediator.Setup(item => item.Send(
                It.IsAny<CreatePaymobPaymentOrderCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<PaymobPaymentOrderResultDto>.Success(
                new PaymobPaymentOrderResultDto(Guid.Empty, "https://paymob.example.test/payment")));
        var accountService = CreateAccountService(attendee);
        var userContext = CreateAttendeeContext(attendee.Id);
        var capturingUnitOfWork = new CapturingUnitOfWork(new UnitOfWork(context));
        var handler = new RegisterForEventCommandHandler(
            capturingUnitOfWork,
            userContext.Object,
            new GenericRepository<Event>(context),
            new GenericRepository<PaymentTransaction>(context),
            new GenericRepository<Registration>(context),
            accountService.Object,
            mediator.Object);

        var result = await handler.Handle(new RegisterationCommand(@event.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, $"Registration failed with {result.ErrorCode}: {result.Message} {capturingUnitOfWork.LastException}");
        var intent = await context.PaymentTransactions.AsNoTracking().SingleAsync();
        Assert.Equal(PaymentTransactionStatus.Pending, intent.Status);
        Assert.Equal(PaymentOrderCreationStatus.Pending, intent.OrderCreationStatus);
        Assert.False(string.IsNullOrWhiteSpace(intent.MerchantOrderId));
        Assert.Null(intent.PaymobOrderId);
        Assert.Equal(1, await context.Registrations.CountAsync());
    }

    [Fact]
    public async Task Provider_failure_marks_existing_payment_intent_as_retryable_failure()
    {
        await using var database = new TestDatabase();
        var seed = await database.SeedPendingPaidRegistrationAsync();
        await using var context = database.CreateContext();
        var paymob = new Mock<IPaymobService>();
        paymob.Setup(item => item.GeneratePaymentLinkAsync(It.IsAny<PaymobPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Provider unavailable"));
        var handler = CreateOrderHandler(context, seed.Attendee, paymob.Object);

        var result = await handler.Handle(new CreatePaymobPaymentOrderCommand(seed.Registration.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCode.PaymentProviderError, result.ErrorCode);
        var intent = await context.PaymentTransactions.AsNoTracking().SingleAsync();
        Assert.Equal(PaymentOrderCreationStatus.Failed, intent.OrderCreationStatus);
        Assert.Equal(1, intent.OrderCreationAttempts);
        Assert.Null(intent.PaymobOrderId);
    }

    [Fact]
    public async Task Failed_order_creation_can_be_retried_without_creating_a_second_payment_intent()
    {
        await using var database = new TestDatabase();
        var seed = await database.SeedPendingPaidRegistrationAsync();
        await using var context = database.CreateContext();
        var paymob = new Mock<IPaymobService>();
        paymob.SetupSequence(item => item.GeneratePaymentLinkAsync(It.IsAny<PaymobPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Provider unavailable"))
            .ReturnsAsync(new PaymobPaymentResponse("https://paymob.example.test/payment", "paymob-order-1"));
        var handler = CreateOrderHandler(context, seed.Attendee, paymob.Object);

        var firstAttempt = await handler.Handle(new CreatePaymobPaymentOrderCommand(seed.Registration.Id), CancellationToken.None);
        var retry = await handler.Handle(new CreatePaymobPaymentOrderCommand(seed.Registration.Id), CancellationToken.None);

        Assert.False(firstAttempt.IsSuccess);
        Assert.True(retry.IsSuccess);
        var intent = await context.PaymentTransactions.AsNoTracking().SingleAsync();
        Assert.Equal(PaymentOrderCreationStatus.Created, intent.OrderCreationStatus);
        Assert.Equal("paymob-order-1", intent.PaymobOrderId);
        Assert.Equal(2, intent.OrderCreationAttempts);
        paymob.Verify(item => item.GeneratePaymentLinkAsync(It.IsAny<PaymobPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Local_persistence_failure_after_provider_success_leaves_durable_reference_for_webhook_reconciliation()
    {
        await using var database = new TestDatabase();
        var seed = await database.SeedPendingPaidRegistrationAsync();
        await using var context = database.CreateContext();
        var paymob = new Mock<IPaymobService>();
        paymob.Setup(item => item.GeneratePaymentLinkAsync(It.IsAny<PaymobPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymobPaymentResponse("https://paymob.example.test/payment", "paymob-order-2"));
        var innerUnitOfWork = new UnitOfWork(context);
        var handler = CreateOrderHandler(context, seed.Attendee, paymob.Object, new FailOnSecondExecuteUnitOfWork(innerUnitOfWork));

        var result = await handler.Handle(new CreatePaymobPaymentOrderCommand(seed.Registration.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var intent = await context.PaymentTransactions.AsNoTracking().SingleAsync();
        Assert.Equal(PaymentOrderCreationStatus.Processing, intent.OrderCreationStatus);
        Assert.Null(intent.PaymobOrderId);
        Assert.False(string.IsNullOrWhiteSpace(intent.MerchantOrderId));

        var mediator = new Mock<IMediator>();
        mediator.Setup(item => item.Send(It.IsAny<QueueRegistrationConfirmationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<bool>.Success(true));
        var webhookHandler = new ProcessPaymobWebhookCommandHandler(
            innerUnitOfWork,
            new GenericRepository<Registration>(context),
            new GenericRepository<PaymentTransaction>(context),
            new GenericRepository<Event>(context),
            mediator.Object);

        var callbackResult = await webhookHandler.Handle(new ProcessPaymobWebhookCommand(new PaymobTransactionObj
        {
            Id = 2002,
            Success = true,
            Pending = false,
            AmountCents = 10000,
            Currency = "EGP",
            Order = new PaymobOrderDetails { Id = 1002, MerchantOrderId = intent.MerchantOrderId }
        }), CancellationToken.None);

        Assert.True(callbackResult.IsSuccess);
        var reconciled = await context.PaymentTransactions.AsNoTracking().SingleAsync();
        Assert.Equal("1002", reconciled.PaymobOrderId);
        Assert.Equal("2002", reconciled.PaymobTransactionId);
        Assert.Equal(PaymentTransactionStatus.Success, reconciled.Status);
    }

    [Fact]
    public async Task Duplicate_paid_registration_reuses_the_existing_intent_without_creating_another_transaction()
    {
        await using var database = new TestDatabase();
        var seed = await database.SeedPendingPaidRegistrationAsync();
        await using var context = database.CreateContext();
        var mediator = new Mock<IMediator>();
        mediator.Setup(item => item.Send(
                It.IsAny<CreatePaymobPaymentOrderCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<PaymobPaymentOrderResultDto>.Success(
                new PaymobPaymentOrderResultDto(seed.Registration.Id, "https://paymob.example.test/payment")));
        var accountService = CreateAccountService(seed.Attendee);
        var userContext = CreateAttendeeContext(seed.Attendee.Id);
        var registrationHandler = new RegisterForEventCommandHandler(
            new UnitOfWork(context),
            userContext.Object,
            new GenericRepository<Event>(context),
            new GenericRepository<PaymentTransaction>(context),
            new GenericRepository<Registration>(context),
            accountService.Object,
            mediator.Object);

        var result = await registrationHandler.Handle(new RegisterationCommand(seed.Event.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await context.PaymentTransactions.CountAsync());
        Assert.Equal(1, await context.Registrations.CountAsync());
    }

    [Fact]
    public async Task Registration_unique_constraint_rejects_a_concurrent_duplicate_paid_registration()
    {
        await using var database = new TestDatabase();
        var seed = await database.SeedPendingPaidRegistrationAsync();
        await using var competingContext = database.CreateContext();
        competingContext.Registrations.Add(new Registration
        {
            EventId = seed.Event.Id,
            UserId = seed.Attendee.Id,
            Status = RegistrationStatus.Pending,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => competingContext.SaveChangesAsync());
    }

    private static CreatePaymobPaymentOrderCommandHandler CreateOrderHandler(
        EventHub.Persistence.Data.Contexts.EventDbContext context,
        ApplicationUser attendee,
        IPaymobService paymobService,
        IUnitOfWork? unitOfWork = null) =>
        new(
            unitOfWork ?? new UnitOfWork(context),
            new GenericRepository<PaymentTransaction>(context),
            CreateAccountService(attendee).Object,
            CreateAttendeeContext(attendee.Id).Object,
            paymobService);

    private static Mock<IAccountService> CreateAccountService(ApplicationUser attendee)
    {
        var accountService = new Mock<IAccountService>();
        accountService.Setup(item => item.GetUserProfileAsync(attendee.Id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<UserProfileResponse>.Success(new UserProfileResponse(
                attendee.Email!,
                attendee.UserName!,
                attendee.FullName,
                attendee.PhoneNumber ?? "+201000000000")));
        return accountService;
    }

    private static Mock<IUserContext> CreateAttendeeContext(Guid attendeeId)
    {
        var userContext = new Mock<IUserContext>();
        userContext.SetupGet(item => item.UserId).Returns(attendeeId);
        userContext.Setup(item => item.IsInRole(RoleNames.Attendee)).Returns(true);
        return userContext;
    }

    private sealed class FailOnSecondExecuteUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        private int _calls;

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken) =>
            ExecuteAsync(action, IsolationLevel.ReadCommitted, cancellationToken);

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            _calls++;
            if (_calls == 2)
                throw new DbUpdateException("Simulated local persistence failure.");

            return inner.ExecuteAsync(action, isolationLevel, cancellationToken);
        }
    }

    private sealed class CapturingUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public Exception? LastException { get; private set; }

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken) =>
            ExecuteAsync(action, IsolationLevel.ReadCommitted, cancellationToken);

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            try
            {
                return await inner.ExecuteAsync(action, isolationLevel, cancellationToken);
            }
            catch (Exception exception)
            {
                LastException = exception;
                throw;
            }
        }
    }
}
