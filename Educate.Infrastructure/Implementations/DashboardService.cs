using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(AppDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<object> GetDashboardDataAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new { };

        var activeSubscriptions = await GetActiveSubscriptionsAsync(userId);
        var notifications = await GetDashboardNotificationsAsync(userId);
        var progressSummary = await GetProgressSummaryAsync(userId);

        return new
        {
            WelcomeMessage = $"Welcome back, {user.FirstName}!",
            ActiveSubscriptions = activeSubscriptions,
            Notifications = notifications,
            ProgressSummary = progressSummary,
            QuickActions = new[]
            {
                new
                {
                    Action = "RenewSubscription",
                    Label = "Renew Subscription",
                    Url = "/renew",
                },
                new
                {
                    Action = "EnrollCourse",
                    Label = "Enroll in New Course",
                    Url = "/courses",
                },
                new
                {
                    Action = "DownloadMaterials",
                    Label = "Download Materials",
                    Url = "/materials",
                },
            },
        };
    }

    public async Task<IEnumerable<object>> GetUserSubscriptionsAsync(string userId)
    {
        return await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc => uc.UserId == userId)
            .Select(uc => new
            {
                uc.UserCourseId,
                CourseName = uc.Course.Name,
                LevelName = uc.Level.Name,
                uc.Status,
                uc.SubscriptionStartDate,
                uc.SubscriptionEndDate,
                DaysRemaining = (uc.SubscriptionEndDate - DateTime.UtcNow).Days,
                CanRenew = uc.Status == "ExpiringSoon"
                    || uc.Status == "Expired"
                    || (uc.SubscriptionEndDate - DateTime.UtcNow).Days <= 30,
                HasAccess = uc.Status == "Active"
                    || uc.Status == "Renewed"
                    || uc.Status == "ExpiringSoon",
            })
            .OrderByDescending(uc => uc.SubscriptionStartDate)
            .ToListAsync();
    }

    public async Task<object> GetProgressSummaryAsync(string userId)
    {
        var progressData = await _context
            .UserProgresses.Include(up => up.Course)
            .Include(up => up.Level)
            .Include(up => up.Subject)
            .Where(up => up.UserId == userId)
            .GroupBy(up => new
            {
                up.CourseId,
                CourseName = up.Course.Name,
                up.LevelId,
                LevelName = up.Level.Name,
            })
            .Select(g => new
            {
                CourseId = g.Key.CourseId,
                CourseName = g.Key.CourseName,
                LevelId = g.Key.LevelId,
                LevelName = g.Key.LevelName,
                TotalSubjects = g.Count(),
                CompletedSubjects = g.Count(p => p.CompletionStatus == "Completed"),
                InProgressSubjects = g.Count(p => p.CompletionStatus == "In Progress"),
                CompletionPercentage = g.Count() > 0
                    ? (g.Count(p => p.CompletionStatus == "Completed") * 100.0 / g.Count())
                    : 0,
                AverageScore = g.Where(p => p.Score.HasValue).Average(p => p.Score) ?? 0,
            })
            .ToListAsync();

        return progressData;
    }

    public async Task<IEnumerable<object>> GetPaymentHistoryAsync(string userId)
    {
        return await _context
            .Payments.Include(p => p.Course)
            .Include(p => p.Level)
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.PaymentId,
                p.Reference,
                p.Provider,
                p.Amount,
                p.Status,
                CourseName = p.Course != null ? p.Course.Name : "N/A",
                LevelName = p.Level != null ? p.Level.Name : "N/A",
                p.CreatedAt,
                CanDownloadReceipt = p.Status == "Success",
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateProgressAsync(
        string userId,
        Guid subjectId,
        string status,
        decimal? score = null
    )
    {
        var subject = await _context
            .Subjects.Include(s => s.Level)
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId);

        if (subject == null)
            return false;

        var progress = await _context.UserProgresses.FirstOrDefaultAsync(up =>
            up.UserId == userId && up.SubjectId == subjectId
        );

        if (progress == null)
        {
            progress = new Domain.Entities.UserProgress
            {
                UserId = userId,
                CourseId = subject.Level.CourseId,
                LevelId = subject.LevelId,
                SubjectId = subjectId,
                CompletionStatus = status,
                Score = score,
            };
            _context.UserProgresses.Add(progress);
        }
        else
        {
            progress.CompletionStatus = status;
            progress.Score = score ?? progress.Score;
            progress.LastAccessed = DateTime.UtcNow;
            progress.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<IEnumerable<object>> GetActiveSubscriptionsAsync(string userId)
    {
        return await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc =>
                uc.UserId == userId
                && (uc.Status == "Active" || uc.Status == "Renewed" || uc.Status == "ExpiringSoon")
            )
            .Select(uc => new
            {
                CourseName = uc.Course.Name,
                LevelName = uc.Level.Name,
                uc.Status,
                uc.SubscriptionEndDate,
                DaysRemaining = (uc.SubscriptionEndDate - DateTime.UtcNow).Days,
            })
            .ToListAsync();
    }

    private async Task<IEnumerable<object>> GetDashboardNotificationsAsync(string userId)
    {
        return await _context
            .Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new
            {
                n.NotificationId,
                n.Title,
                n.Message,
                n.Type,
                n.CreatedAt,
            })
            .ToListAsync();
    }
}
