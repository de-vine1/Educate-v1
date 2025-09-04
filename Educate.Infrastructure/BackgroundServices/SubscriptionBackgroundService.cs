using Educate.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.BackgroundServices;

public class SubscriptionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1); // Run daily

    public SubscriptionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionBackgroundService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSubscriptionsAsync();
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in subscription background service");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Wait 30 minutes on error
            }
        }
    }

    private async Task ProcessSubscriptionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        _logger.LogInformation("Starting subscription processing at {Time}", DateTime.UtcNow);

        await subscriptionService.UpdateSubscriptionStatusesAsync();
        await subscriptionService.CheckExpiredSubscriptionsAsync();
        await subscriptionService.NotifyExpiringSubscriptionsAsync();

        _logger.LogInformation("Completed subscription processing at {Time}", DateTime.UtcNow);
    }
}
