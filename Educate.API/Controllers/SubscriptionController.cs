using System.Security.Claims;
using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(AppDbContext context, ISubscriptionService subscriptionService)
    {
        _context = context;
        _subscriptionService = subscriptionService;
    }

    [HttpGet("my-subscriptions")]
    public async Task<IActionResult> GetMySubscriptions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var subscriptions = await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc => uc.UserId == userId)
            .Select(uc => new
            {
                uc.UserCourseId,
                CourseName = uc.Course.Name,
                LevelName = uc.Level.Name,
                uc.SubscriptionStartDate,
                uc.SubscriptionEndDate,
                uc.Status,
                DaysRemaining = uc.Status == "Active"
                    ? (uc.SubscriptionEndDate - DateTime.UtcNow).Days
                    : 0,
            })
            .OrderByDescending(uc => uc.SubscriptionStartDate)
            .ToListAsync();

        return Ok(subscriptions);
    }

    [HttpPost("check-expired")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CheckExpiredSubscriptions()
    {
        await _subscriptionService.CheckExpiredSubscriptionsAsync();
        return Ok(new { message = "Expired subscriptions check completed" });
    }

    [HttpPost("notify-expiring")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> NotifyExpiringSubscriptions()
    {
        await _subscriptionService.NotifyExpiringSubscriptionsAsync();
        return Ok(new { message = "Expiring subscription notifications sent" });
    }
}
