using Educate.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Services;

public class SubscriptionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionBackgroundService> _logger;

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
                using var scope = _serviceProvider.CreateScope();
                var subscriptionService =
                    scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

                await subscriptionService.CheckExpiredSubscriptionsAsync();
                await subscriptionService.NotifyExpiringSubscriptionsAsync();

                _logger.LogInformation("Subscription check completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during subscription check");
            }

            // Run every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
