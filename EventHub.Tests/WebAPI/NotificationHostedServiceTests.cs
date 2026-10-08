using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Notifications.DispatchPendingNotifications;
using EventHub.Application.Features.Notifications.QueueDueEventReminders;
using EventHub.WebAPI.Presentation.HostedServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR;
using Moq;
using Xunit;

namespace EventHub.Tests.WebAPI;

[Trait("Category", "MockUnit")]
public class NotificationHostedServiceTests
{
    [Fact]
    public async Task Start_QueuesRemindersBeforeDispatchAndStopsWithoutWaitingForTimer()
    {
        // Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var sequence = new MockSequence();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        mediator.InSequence(sequence).Setup(m => m.Send(It.IsAny<QueueDueEventRemindersCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<int>.Success(2));
        mediator.InSequence(sequence).Setup(m => m.Send(It.IsAny<DispatchPendingNotificationsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => { dispatched.TrySetResult(); return Task.FromResult(RequestResult<int>.Success(2)); });
        using var services = new ServiceCollection().AddScoped(_ => mediator.Object).BuildServiceProvider();
        using var worker = new NotificationHostedService(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<NotificationHostedService>.Instance);

        // Act
        await worker.StartAsync(default);
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(default);

        // Assert
        mediator.Verify(m => m.Send(It.IsAny<QueueDueEventRemindersCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(It.IsAny<DispatchPendingNotificationsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartupFailure_IsContainedAndDoesNotDispatch()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var called = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        mediator.Setup(m => m.Send(It.IsAny<QueueDueEventRemindersCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => { called.TrySetResult(); return Task.FromException<RequestResult<int>>(new InvalidOperationException("simulated")); });
        using var services = new ServiceCollection().AddScoped(_ => mediator.Object).BuildServiceProvider();
        using var worker = new NotificationHostedService(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<NotificationHostedService>.Instance);

        // Act
        await worker.StartAsync(default);
        await called.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(default);

        // Assert
        mediator.Verify(m => m.Send(It.IsAny<DispatchPendingNotificationsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
