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
            .Where(uc =>
                (uc.Status == "Active" || uc.Status == "ExpiringSoon" || uc.Status == "Renewed")
                && uc.SubscriptionEndDate <= DateTime.UtcNow
            )
            .ToListAsync();

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = "Expired";
            subscription.UpdatedAt = DateTime.UtcNow;

            // Log expiry in history
            var history = new Domain.Entities.SubscriptionHistory
            {
                SubscriptionId = subscription.UserCourseId,
                UserId = subscription.UserId,
                CourseId = subscription.CourseId,
                LevelId = subscription.LevelId,
                Action = "Expired",
                PreviousEndDate = subscription.SubscriptionEndDate,
                NewEndDate = subscription.SubscriptionEndDate
            };
            _context.SubscriptionHistories.Add(history);

            // Send expiry notification
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendExpiryReminderAsync(
                        subscription.UserId,
                        subscription.Course.Name,
                        subscription.Level.Name,
                        subscription.SubscriptionEndDate,
                        0
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
                (uc.Status == "Active" || uc.Status == "Renewed")
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

    public async Task<bool> HasActiveSubscriptionAsync(string userId, int courseId, int levelId)
    {
        return await _context.UserCourses.AnyAsync(uc =>
            uc.UserId == userId
            && uc.CourseId == courseId
            && uc.LevelId == levelId
            && (uc.Status == "Active" || uc.Status == "ExpiringSoon" || uc.Status == "Renewed")
            && uc.SubscriptionEndDate > DateTime.UtcNow
        );
    }

    public async Task<IEnumerable<object>> GetUserActiveSubscriptionsAsync(string userId)
    {
        return await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc =>
                uc.UserId == userId
                && (uc.Status == "Active" || uc.Status == "ExpiringSoon" || uc.Status == "Renewed")
            )
            .Select(uc => new
            {
                uc.UserCourseId,
                CourseName = uc.Course.Name,
                LevelName = uc.Level.Name,
                uc.SubscriptionStartDate,
                uc.SubscriptionEndDate,
                uc.Status,
                uc.RenewalCount,
                DaysRemaining = (uc.SubscriptionEndDate - DateTime.UtcNow).Days,
            })
            .ToListAsync();
    }

    public async Task<bool> RenewSubscriptionAsync(
        string userId,
        int courseId,
        int levelId,
        string paymentReference
    )
    {
        var existingSubscription = await _context.UserCourses.FirstOrDefaultAsync(uc =>
            uc.UserId == userId && uc.CourseId == courseId && uc.LevelId == levelId
        );

        if (existingSubscription == null)
            return false;

        var previousEndDate = existingSubscription.SubscriptionEndDate;
        var newEndDate = DateTime.UtcNow.AddMonths(6);

        // Update existing subscription
        existingSubscription.Status = "Renewed";
        existingSubscription.SubscriptionStartDate = DateTime.UtcNow;
        existingSubscription.SubscriptionEndDate = newEndDate;
        existingSubscription.RenewalCount += 1;
        existingSubscription.UpdatedAt = DateTime.UtcNow;

        // Log renewal in history
        var history = new Domain.Entities.SubscriptionHistory
        {
            SubscriptionId = existingSubscription.UserCourseId,
            UserId = userId,
            CourseId = courseId,
            LevelId = levelId,
            Action = "Renewed",
            PaymentReference = paymentReference,
            PreviousEndDate = previousEndDate,
            NewEndDate = newEndDate
        };
        _context.SubscriptionHistories.Add(history);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Renewed subscription for user {UserId}, course {CourseId}, level {LevelId}",
            userId,
            courseId,
            levelId
        );

        return true;
    }

    public async Task UpdateSubscriptionStatusesAsync()
    {
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(14);

        // Mark subscriptions as ExpiringSoon (14 days before expiry)
        var soonToExpire = await _context
            .UserCourses.Where(uc =>
                (uc.Status == "Active" || uc.Status == "Renewed")
                && uc.SubscriptionEndDate <= expiringThreshold
                && uc.SubscriptionEndDate > now
            )
            .ToListAsync();

        foreach (var subscription in soonToExpire)
        {
            subscription.Status = "ExpiringSoon";
            subscription.UpdatedAt = now;
        }

        // Mark expired subscriptions
        var expired = await _context
            .UserCourses.Where(uc =>
                (uc.Status == "Active" || uc.Status == "ExpiringSoon" || uc.Status == "Renewed")
                && uc.SubscriptionEndDate <= now
            )
            .ToListAsync();

        foreach (var subscription in expired)
        {
            subscription.Status = "Expired";
            subscription.UpdatedAt = now;
        }

        if (soonToExpire.Any() || expired.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Updated {ExpiringSoon} subscriptions to ExpiringSoon and {Expired} to Expired",
                soonToExpire.Count,
                expired.Count
            );
        }
    }



    public async Task<IEnumerable<object>> GetAllSubscriptionsAsync()
    {
        return await _context.UserCourses
            .Include(uc => uc.User)
            .Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Select(uc => new
            {
                uc.UserCourseId,
                UserId = uc.UserId,
                UserName = uc.User.FirstName + " " + uc.User.LastName,
                Email = uc.User.Email,
                CourseName = uc.Course.Name,
                LevelName = uc.Level.Name,
                uc.SubscriptionStartDate,
                uc.SubscriptionEndDate,
                uc.Status,
                uc.RenewalCount,
                DaysRemaining = (uc.SubscriptionEndDate - DateTime.UtcNow).Days
            })
            .OrderByDescending(uc => uc.SubscriptionStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<object>> GetUserRenewalHistoryAsync(string userId)
    {
        return await _context.SubscriptionHistories
            .Include(sh => sh.Course)
            .Include(sh => sh.Level)
            .Where(sh => sh.UserId == userId)
            .Select(sh => new
            {
                sh.Id,
                sh.Action,
                CourseName = sh.Course.Name,
                LevelName = sh.Level.Name,
                sh.PaymentReference,
                sh.PaymentProvider,
                sh.Amount,
                sh.PreviousEndDate,
                sh.NewEndDate,
                sh.CreatedAt
            })
            .OrderByDescending(sh => sh.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasActiveSubscriptionAsync(string userId, int subjectId)
    {
        var subject = await _context.Subjects.Include(s => s.Level)
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId);
        
        if (subject == null)
            return false;

        return await _context.UserCourses.AnyAsync(uc =>
            uc.UserId == userId
            && uc.CourseId == subject.Level.CourseId
            && uc.LevelId == subject.LevelId
            && (uc.Status == "Active" || uc.Status == "ExpiringSoon" || uc.Status == "Renewed")
            && uc.SubscriptionEndDate > DateTime.UtcNow
        );
    }

    public async Task<bool> HasActiveSubscriptionForLevelAsync(string userId, int courseId, int levelId)
    {
        return await _context.UserCourses.AnyAsync(uc =>
            uc.UserId == userId
            && uc.CourseId == courseId
            && uc.LevelId == levelId
            && (uc.Status == "Active" || uc.Status == "ExpiringSoon" || uc.Status == "Renewed")
            && uc.SubscriptionEndDate > DateTime.UtcNow
        );
    }
}
