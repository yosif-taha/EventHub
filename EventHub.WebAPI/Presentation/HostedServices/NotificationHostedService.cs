using EventHub.Application.Features.Notifications.DispatchPendingNotifications;
using EventHub.Application.Features.Notifications.QueueDueEventReminders;
using MediatR;

namespace EventHub.WebAPI.Presentation.HostedServices
{
    public class NotificationHostedService(
        IServiceScopeFactory _scopeFactory,
        ILogger<NotificationHostedService> _logger) : BackgroundService
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(5);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ProcessAsync(stoppingToken);

            using var timer = new PeriodicTimer(PollingInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await ProcessAsync(stoppingToken);
        }

        private async Task ProcessAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.Send(new QueueDueEventRemindersCommand(), cancellationToken);
                await mediator.Send(new DispatchPendingNotificationsCommand(), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to process event notifications.");
            }
        }
    }
}
