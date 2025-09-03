using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetDashboardOverview()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var activeSubscriptions = await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc =>
                uc.UserId == userId
                && uc.Status == "Active"
                && uc.SubscriptionEndDate > DateTime.UtcNow
            )
            .Select(uc => new
            {
                uc.UserCourseId,
                CourseName = uc.Course.Name,
                LevelName = uc.Level.Name,
                uc.SubscriptionEndDate,
                DaysRemaining = (uc.SubscriptionEndDate.Date - DateTime.UtcNow.Date).Days,
                IsExpiringSoon = (uc.SubscriptionEndDate.Date - DateTime.UtcNow.Date).Days <= 7,
            })
            .ToListAsync();

        var expiredSubscriptions = await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc =>
                uc.UserId == userId
                && (uc.Status == "Expired" || uc.SubscriptionEndDate <= DateTime.UtcNow)
            )
            .CountAsync();

        var totalSpent = await _context
            .Payments.Where(p => p.UserId == userId && p.Status == "Success")
            .SumAsync(p => p.Amount);

        return Ok(
            new
            {
                ActiveSubscriptions = activeSubscriptions,
                ExpiredCount = expiredSubscriptions,
                TotalActiveCount = activeSubscriptions.Count,
                TotalSpent = totalSpent,
                ExpiringSoonCount = activeSubscriptions.Count(s => s.IsExpiringSoon),
            }
        );
    }

    [HttpGet("available-courses")]
    public async Task<IActionResult> GetAvailableCourses()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Get user's current enrollments
        var userEnrollments = await _context
            .UserCourses.Where(uc => uc.UserId == userId && uc.Status == "Active")
            .Select(uc => new { uc.CourseId, uc.LevelId })
            .ToListAsync();

        var courses = await _context
            .Courses.Include(c => c.Levels)
            .ThenInclude(l => l.Subjects)
            .Select(c => new
            {
                c.CourseId,
                c.Name,
                c.Description,
                Levels = c
                    .Levels.OrderBy(l => l.Order)
                    .Select(l => new
                    {
                        l.LevelId,
                        l.Name,
                        l.Order,
                        SubjectCount = l.Subjects.Count(),
                        IsEnrolled = userEnrollments.Any(ue =>
                            ue.CourseId == c.CourseId && ue.LevelId == l.LevelId
                        ),
                        Price = 50000, // Fixed price for now
                    }),
            })
            .ToListAsync();

        return Ok(courses);
    }
}
