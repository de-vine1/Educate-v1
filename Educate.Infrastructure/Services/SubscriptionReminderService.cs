using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Services;

public class SubscriptionReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionReminderService> _logger;

    public SubscriptionReminderService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionReminderService> logger
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
                await CheckExpiringSubscriptions();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscription reminder service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry in 1 hour on error
            }
        }
    }

    private async Task CheckExpiringSubscriptions()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DateTime.UtcNow.Date;
        var sevenDaysFromNow = today.AddDays(7);
        var oneDayFromNow = today.AddDays(1);

        // Get subscriptions expiring in 7 days or 1 day
        var expiringSubscriptions = await context
            .UserCourses.Include(uc => uc.User)
            .Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc =>
                uc.Status == "Active"
                && (
                    uc.SubscriptionEndDate.Date == sevenDaysFromNow
                    || uc.SubscriptionEndDate.Date == oneDayFromNow
                )
            )
            .ToListAsync();

        foreach (var subscription in expiringSubscriptions)
        {
            var daysRemaining = (subscription.SubscriptionEndDate.Date - today).Days;

            // Check if we already sent notification for this subscription and timeframe
            var existingNotification = await context.Notifications.AnyAsync(n =>
                n.UserId == subscription.UserId
                && n.Type == "SUBSCRIPTION_EXPIRY"
                && n.Message.Contains(subscription.Course.Name)
                && n.Message.Contains(subscription.Level.Name)
                && n.CreatedAt.Date == today
            );

            if (!existingNotification)
            {
                await notificationService.SendExpiryReminderAsync(
                    subscription.UserId,
                    subscription.Course.Name,
                    subscription.Level.Name,
                    subscription.SubscriptionEndDate,
                    daysRemaining
                );

                _logger.LogInformation(
                    $"Sent expiry reminder to user {subscription.UserId} for {subscription.Course.Name} - {subscription.Level.Name}"
                );
            }
        }

        // Update expired subscriptions
        var expiredSubscriptions = await context
            .UserCourses.Where(uc => uc.Status == "Active" && uc.SubscriptionEndDate.Date < today)
            .ToListAsync();

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = "Expired";
        }

        if (expiredSubscriptions.Any())
        {
            await context.SaveChangesAsync();
            _logger.LogInformation($"Updated {expiredSubscriptions.Count} expired subscriptions");
        }
    }
}
