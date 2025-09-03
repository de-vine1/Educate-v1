using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        AppDbContext context,
        IEmailService emailService,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task CheckExpiredSubscriptionsAsync()
    {
        var expiredSubscriptions = await _context.UserCourses
            .Include(uc => uc.User)
            .Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc => uc.Status == "Active" && uc.SubscriptionEndDate <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = "Expired";
            subscription.UpdatedAt = DateTime.UtcNow;

            // Send expiration notification email
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        subscription.User.Email!,
                        "Subscription Expired - Educate Platform",
                        $"Your subscription to {subscription.Course.Name} - {subscription.Level.Name} has expired. Please renew to continue accessing course materials."
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send expiration email to user {UserId}", subscription.UserId);
                }
            });

            _logger.LogInformation("Marked subscription {UserCourseId} as expired for user {UserId}", 
                subscription.UserCourseId, subscription.UserId);
        }

        if (expiredSubscriptions.Any())
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task NotifyExpiringSubscriptionsAsync()
    {
        var expiringDate = DateTime.UtcNow.AddDays(7); // Notify 7 days before expiration
        
        var expiringSubscriptions = await _context.UserCourses
            .Include(uc => uc.User)
            .Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc => uc.Status == "Active" && 
                        uc.SubscriptionEndDate <= expiringDate && 
                        uc.SubscriptionEndDate > DateTime.UtcNow)
            .ToListAsync();

        foreach (var subscription in expiringSubscriptions)
        {
            var daysLeft = (subscription.SubscriptionEndDate - DateTime.UtcNow).Days;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        subscription.User.Email!,
                        "Subscription Expiring Soon - Educate Platform",
                        $"Your subscription to {subscription.Course.Name} - {subscription.Level.Name} expires in {daysLeft} days. Renew now to avoid interruption."
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send expiring notification to user {UserId}", subscription.UserId);
                }
            });

            _logger.LogInformation("Sent expiring notification for subscription {UserCourseId} to user {UserId}", 
                subscription.UserCourseId, subscription.UserId);
        }
    }

    public async Task<bool> HasActiveSubscriptionAsync(string userId, Guid courseId, Guid levelId)
    {
        return await _context.UserCourses
            .AnyAsync(uc => uc.UserId == userId && 
                           uc.CourseId == courseId && 
                           uc.LevelId == levelId && 
                           uc.Status == "Active" && 
                           uc.SubscriptionEndDate > DateTime.UtcNow);
    }

    public async Task<IEnumerable<object>> GetUserActiveSubscriptionsAsync(string userId)
    {
        return await _context.UserCourses
            .Include(uc => uc.Course)
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
                DaysRemaining = (uc.SubscriptionEndDate - DateTime.UtcNow).Days
            })
            .ToListAsync();
    }
}
