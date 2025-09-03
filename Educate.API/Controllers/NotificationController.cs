using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _context;

    public NotificationController(INotificationService notificationService, AppDbContext context)
    {
        _notificationService = notificationService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        return Ok(notifications);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verify notification belongs to user
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null || notification.UserId != userId)
            return NotFound();

        await _notificationService.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpGet("subscription-status")]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subscriptions = await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc => uc.UserId == userId && uc.Status == "Active")
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

        return Ok(subscriptions);
    }
}
