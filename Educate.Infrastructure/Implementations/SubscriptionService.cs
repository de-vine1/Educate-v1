using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        AppDbContext context,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<SubscriptionService> logger
    )
    {
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task CheckExpiredSubscriptionsAsync()
    {
        var expiredSubscriptions = await _context
            .UserCourses.Include(uc => uc.User)
            .Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc => uc.Status == "Active" && uc.SubscriptionEndDate <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = "Expired";
            subscription.UpdatedAt = DateTime.UtcNow;

            // Use notification service for expiry notifications
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendExpiryReminderAsync(
                        subscription.UserId,
                        subscription.Course.Name,
                        subscription.Level.Name,
                        subscription.SubscriptionEndDate,
                        0 // Already expired
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send expiry notification to user {UserId}",
                        subscription.UserId
                    );
                }
            });

            _logger.LogInformation(
                "Marked subscription {UserCourseId} as expired for user {UserId}",
                subscription.UserCourseId,
                subscription.UserId
            );
        }

        if (expiredSubscriptions.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task NotifyExpiringSubscriptionsAsync()
    {
        // Send 2-week warnings
        await NotifyExpiringSubscriptionsForPeriodAsync(14);

        // Send 1-week warnings
        await NotifyExpiringSubscriptionsForPeriodAsync(7);
    }

    private async Task NotifyExpiringSubscriptionsForPeriodAsync(int daysBeforeExpiry)
    {
        var expiringDate = DateTime.UtcNow.AddDays(daysBeforeExpiry);

        var expiringSubscriptions = await _context
            .UserCourses.Include(uc => uc.User)
            .Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc =>
                uc.Status == "Active"
                && uc.SubscriptionEndDate <= expiringDate
                && uc.SubscriptionEndDate > DateTime.UtcNow
            )
            .ToListAsync();

        foreach (var subscription in expiringSubscriptions)
        {
            var daysLeft = (subscription.SubscriptionEndDate - DateTime.UtcNow).Days;

            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendExpiryReminderAsync(
                        subscription.UserId,
                        subscription.Course.Name,
                        subscription.Level.Name,
                        subscription.SubscriptionEndDate,
                        daysLeft
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send expiring notification to user {UserId}",
                        subscription.UserId
                    );
                }
            });

            _logger.LogInformation(
                "Sent {Days}-day expiring notification for subscription {UserCourseId} to user {UserId}",
                daysBeforeExpiry,
                subscription.UserCourseId,
                subscription.UserId
            );
        }
    }

    public async Task<bool> HasActiveSubscriptionAsync(string userId, Guid courseId, Guid levelId)
    {
        return await _context.UserCourses.AnyAsync(uc =>
            uc.UserId == userId
            && uc.CourseId == courseId
            && uc.LevelId == levelId
            && uc.Status == "Active"
            && uc.SubscriptionEndDate > DateTime.UtcNow
        );
    }

    public async Task<IEnumerable<object>> GetUserActiveSubscriptionsAsync(string userId)
    {
        return await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc => uc.UserId == userId && uc.Status == "Active")
            .Select(uc => new
            {
                uc.UserCourseId,
                CourseName = uc.Course.Name,
                LevelName = uc.Level.Name,
                uc.SubscriptionStartDate,
                uc.SubscriptionEndDate,
                uc.Status,
                DaysRemaining = (uc.SubscriptionEndDate - DateTime.UtcNow).Days,
            })
            .ToListAsync();
    }
}
