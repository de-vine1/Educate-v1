using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class StudentManagementService : IStudentManagementService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<StudentManagementService> _logger;

    public StudentManagementService(AppDbContext context, INotificationService notificationService, 
        IEmailService emailService, ILogger<StudentManagementService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> GetAllStudentsAsync(string? searchTerm = null, string? courseFilter = null, string? statusFilter = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => u.FirstName.Contains(searchTerm) || u.LastName.Contains(searchTerm) || u.Email!.Contains(searchTerm));
        }

        var students = await query
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.CreatedAt,
                ActiveSubscriptions = _context.UserCourses
                    .Where(uc => uc.UserId == u.Id && (uc.Status == "Active" || uc.Status == "Renewed" || uc.Status == "ExpiringSoon"))
                    .Include(uc => uc.Course)
                    .Include(uc => uc.Level)
                    .Select(uc => new
                    {
                        CourseName = uc.Course.Name,
                        LevelName = uc.Level.Name,
                        uc.Status,
                        uc.SubscriptionEndDate
                    })
                    .ToList(),
                TotalTestAttempts = _context.UserTestAttempts.Count(a => a.UserId == u.Id && a.IsCompleted),
                AverageScore = _context.UserTestAttempts
                    .Where(a => a.UserId == u.Id && a.IsCompleted)
                    .Average(a => (double?)a.Score) ?? 0
            })
            .ToListAsync();

        if (!string.IsNullOrEmpty(courseFilter))
        {
            students = students.Where(s => s.ActiveSubscriptions.Any(sub => sub.CourseName.Contains(courseFilter))).ToList();
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            students = statusFilter.ToLower() switch
            {
                "active" => students.Where(s => s.ActiveSubscriptions.Any()).ToList(),
                "inactive" => students.Where(s => !s.ActiveSubscriptions.Any()).ToList(),
                _ => students
            };
        }

        return students;
    }

    public async Task<object> GetStudentDetailsAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new { Success = false, Message = "Student not found" };

        var subscriptions = await _context.UserCourses
            .Include(uc => uc.Course)
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
                uc.RenewalCount,
                IsScholarship = false
            })
            .ToListAsync();

        var testHistory = await _context.UserTestAttempts
            .Include(a => a.Course)
            .Include(a => a.Level)
            .Include(a => a.Subject)
            .Where(a => a.UserId == userId && a.IsCompleted)
            .Select(a => new
            {
                a.AttemptId,
                a.TestType,
                CourseName = a.Course.Name,
                LevelName = a.Level.Name,
                SubjectName = a.Subject != null ? a.Subject.Name : "Full Exam",
                a.Score,
                a.AttemptDate,
                a.TimeTaken
            })
            .OrderByDescending(a => a.AttemptDate)
            .ToListAsync();

        var paymentHistory = await _context.Payments
            .Include(p => p.Course)
            .Include(p => p.Level)
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.PaymentId,
                p.Reference,
                p.Amount,
                p.Status,
                CourseName = p.Course != null ? p.Course.Name : "N/A",
                LevelName = p.Level != null ? p.Level.Name : "N/A",
                p.CreatedAt
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return new
        {
            Success = true,
            Student = new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.CreatedAt
            },
            Subscriptions = subscriptions,
            TestHistory = testHistory,
            PaymentHistory = paymentHistory,
            Statistics = new
            {
                TotalTestAttempts = testHistory.Count,
                AverageScore = testHistory.Any() ? testHistory.Average(t => t.Score) : 0,
                BestScore = testHistory.Any() ? testHistory.Max(t => t.Score) : 0,
                TotalPayments = paymentHistory.Where(p => p.Status == "Success").Sum(p => p.Amount)
            }
        };
    }

    public async Task<bool> ExtendSubscriptionAsync(string userId, Guid courseId, Guid levelId, int months, string adminId)
    {
        var subscription = await _context.UserCourses
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CourseId == courseId && uc.LevelId == levelId);

        if (subscription == null)
            return false;

        var previousEndDate = subscription.SubscriptionEndDate;
        subscription.SubscriptionEndDate = subscription.SubscriptionEndDate.AddMonths(months);
        subscription.Status = "Active";
        subscription.UpdatedAt = DateTime.UtcNow;

        var history = new SubscriptionHistory
        {
            SubscriptionId = subscription.UserCourseId,
            UserId = userId,
            CourseId = courseId,
            LevelId = levelId,
            Action = "Extended",
            PreviousEndDate = previousEndDate,
            NewEndDate = subscription.SubscriptionEndDate
        };
        _context.SubscriptionHistories.Add(history);

        await _context.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                await _notificationService.SendSubscriptionExtensionNotificationAsync(userId, months);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send extension notification to user {UserId}", userId);
            }
        });

        return true;
    }

    public async Task<bool> ToggleScholarshipAsync(string userId, Guid courseId, Guid levelId, string adminId)
    {
        var subscription = await _context.UserCourses
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CourseId == courseId && uc.LevelId == levelId);

        if (subscription == null)
            return false;

        subscription.UpdatedAt = DateTime.UtcNow;

        var history = new SubscriptionHistory
        {
            SubscriptionId = subscription.UserCourseId,
            UserId = userId,
            CourseId = courseId,
            LevelId = levelId,
            Action = "Scholarship Toggled"
        };
        _context.SubscriptionHistories.Add(history);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetTestAttemptsAsync(string userId, Guid? courseId = null, string adminId = "")
    {
        var query = _context.UserTestAttempts.Where(a => a.UserId == userId);
        
        if (courseId.HasValue)
        {
            query = query.Where(a => a.CourseId == courseId);
        }

        var attempts = await query.ToListAsync();
        _context.UserTestAttempts.RemoveRange(attempts);

        var sessions = await _context.TestSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();
        
        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.IsCompleted = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reset {Count} test attempts for user {UserId} by admin {AdminId}", 
            attempts.Count, userId, adminId);

        return true;
    }

    public async Task<object> SendAnnouncementAsync(string title, string message, string? targetCourse = null, 
        string? targetLevel = null, string? targetUserId = null, string adminId = "")
    {
        var recipients = new List<string>();

        if (!string.IsNullOrEmpty(targetUserId))
        {
            recipients.Add(targetUserId);
        }
        else
        {
            var query = _context.UserCourses.AsQueryable();

            if (!string.IsNullOrEmpty(targetCourse))
            {
                query = query.Where(uc => uc.Course.Name == targetCourse);
            }

            if (!string.IsNullOrEmpty(targetLevel))
            {
                query = query.Where(uc => uc.Level.Name == targetLevel);
            }

            recipients = await query
                .Where(uc => uc.Status == "Active" || uc.Status == "Renewed" || uc.Status == "ExpiringSoon")
                .Select(uc => uc.UserId)
                .Distinct()
                .ToListAsync();
        }

        var sentCount = 0;
        foreach (var userId in recipients)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = "Announcement"
                };
                _context.Notifications.Add(notification);

                var user = await _context.Users.FindAsync(userId);
                if (user != null && user.Email != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendAnnouncementEmailAsync(user.Email, user.FirstName, title, message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send announcement email to {Email}", user.Email);
                        }
                    });
                }

                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send announcement to user {UserId}", userId);
            }
        }

        await _context.SaveChangesAsync();

        return new
        {
            Success = true,
            RecipientsCount = sentCount,
            Message = $"Announcement sent to {sentCount} users"
        };
    }
}