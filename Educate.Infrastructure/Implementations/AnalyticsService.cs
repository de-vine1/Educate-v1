using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(AppDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<object> GetDashboardMetricsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeSubscriptions = await _context.UserCourses.CountAsync(uc =>
            uc.Status == "Active" || uc.Status == "Renewed" || uc.Status == "ExpiringSoon"
        );

        var thisMonthRevenue = await _context
            .Payments.Where(p =>
                p.Status == "Success" && p.CreatedAt >= DateTime.UtcNow.AddDays(-30)
            )
            .SumAsync(p => p.Amount);

        var totalTests = await _context.UserTestAttempts.CountAsync(a => a.IsCompleted);
        var avgTestScore =
            await _context
                .UserTestAttempts.Where(a => a.IsCompleted)
                .AverageAsync(a => (double?)a.Score) ?? 0;

        return new
        {
            TotalUsers = totalUsers,
            ActiveSubscriptions = activeSubscriptions,
            ThisMonthRevenue = thisMonthRevenue,
            TotalTestsCompleted = totalTests,
            AverageTestScore = Math.Round(avgTestScore, 2),
            NewUsersThisWeek = await _context.Users.CountAsync(u =>
                u.CreatedAt >= DateTime.UtcNow.AddDays(-7)
            ),
            ExpiringSubscriptions = await _context.UserCourses.CountAsync(uc =>
                uc.Status == "ExpiringSoon"
            ),
        };
    }

    public async Task<object> GetRevenueAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null
    )
    {
        startDate ??= DateTime.UtcNow.AddMonths(-6);
        endDate ??= DateTime.UtcNow;

        var payments = await _context
            .Payments.Where(p =>
                p.Status == "Success" && p.CreatedAt >= startDate && p.CreatedAt <= endDate
            )
            .Include(p => p.Course)
            .ToListAsync();

        var monthlyRevenue = payments
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(p => p.Amount),
                TransactionCount = g.Count(),
            })
            .OrderBy(x => x.Month)
            .ToList();

        var providerBreakdown = payments
            .GroupBy(p => p.Provider)
            .Select(g => new
            {
                Provider = g.Key,
                Revenue = g.Sum(p => p.Amount),
                Count = g.Count(),
                Percentage = Math.Round(
                    (double)g.Sum(p => p.Amount) / (double)payments.Sum(p => p.Amount) * 100,
                    2
                ),
            })
            .ToList();

        return new
        {
            TotalRevenue = payments.Sum(p => p.Amount),
            MonthlyRevenue = monthlyRevenue,
            ProviderBreakdown = providerBreakdown,
            AverageTransactionValue = payments.Any() ? payments.Average(p => p.Amount) : 0,
        };
    }

    public async Task<object> GetUserEngagementMetricsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context
            .UserCourses.Where(uc =>
                uc.Status == "Active" || uc.Status == "Renewed" || uc.Status == "ExpiringSoon"
            )
            .Select(uc => uc.UserId)
            .Distinct()
            .CountAsync();

        var renewalRate = await _context.UserCourses.Where(uc => uc.RenewalCount > 0).CountAsync();

        var totalSubscriptions = await _context.UserCourses.CountAsync();

        return new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            EngagementRate = totalUsers > 0
                ? Math.Round((double)activeUsers / totalUsers * 100, 2)
                : 0,
            RenewalRate = totalSubscriptions > 0
                ? Math.Round((double)renewalRate / totalSubscriptions * 100, 2)
                : 0,
            TestParticipationRate = await GetTestParticipationRateAsync(),
        };
    }

    public async Task<object> GetCoursePerformanceAsync()
    {
        var courseStats = await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .GroupBy(uc => new { CourseName = uc.Course.Name, LevelName = uc.Level.Name })
            .Select(g => new
            {
                Course = g.Key.CourseName,
                Level = g.Key.LevelName,
                TotalEnrollments = g.Count(),
                ActiveSubscriptions = g.Count(uc =>
                    uc.Status == "Active" || uc.Status == "Renewed" || uc.Status == "ExpiringSoon"
                ),
                Revenue = g.Sum(uc =>
                    _context
                        .Payments.Where(p =>
                            p.CourseId == uc.CourseId
                            && p.LevelId == uc.LevelId
                            && p.Status == "Success"
                        )
                        .Sum(p => p.Amount)
                ),
            })
            .OrderByDescending(x => x.TotalEnrollments)
            .ToListAsync();

        return courseStats;
    }

    public async Task<object> GetTestAnalyticsAsync()
    {
        var testStats = await _context
            .UserTestAttempts.Include(a => a.Course)
            .Include(a => a.Level)
            .Include(a => a.Subject)
            .Where(a => a.IsCompleted)
            .GroupBy(a => new
            {
                a.TestType,
                CourseName = a.Course.Name,
                LevelName = a.Level.Name,
            })
            .Select(g => new
            {
                TestType = g.Key.TestType,
                Course = g.Key.CourseName,
                Level = g.Key.LevelName,
                TotalAttempts = g.Count(),
                AverageScore = g.Average(a => a.Score),
                PassRate = g.Count(a => a.Score >= 50) * 100.0 / g.Count(),
            })
            .OrderByDescending(x => x.TotalAttempts)
            .ToListAsync();

        return testStats;
    }

    public async Task<IEnumerable<object>> GetTopPerformingStudentsAsync(int count = 10)
    {
        return await _context
            .UserTestAttempts.Include(a => a.User)
            .Where(a => a.IsCompleted)
            .GroupBy(a => new
            {
                a.UserId,
                UserName = a.User.FirstName + " " + a.User.LastName,
                a.User.Email,
            })
            .Select(g => new
            {
                UserId = g.Key.UserId,
                UserName = g.Key.UserName,
                Email = g.Key.Email,
                TotalAttempts = g.Count(),
                AverageScore = g.Average(a => a.Score),
                BestScore = g.Max(a => a.Score),
                TestsPassedCount = g.Count(a => a.Score >= 50),
            })
            .OrderByDescending(x => x.AverageScore)
            .Take(count)
            .ToListAsync();
    }

    private async Task<double> GetTestParticipationRateAsync()
    {
        var activeUsers = await _context
            .UserCourses.Where(uc =>
                uc.Status == "Active" || uc.Status == "Renewed" || uc.Status == "ExpiringSoon"
            )
            .Select(uc => uc.UserId)
            .Distinct()
            .CountAsync();

        var usersWithTests = await _context
            .UserTestAttempts.Select(a => a.UserId)
            .Distinct()
            .CountAsync();

        return activeUsers > 0 ? Math.Round((double)usersWithTests / activeUsers * 100, 2) : 0;
    }
}
