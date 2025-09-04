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
                uc.RenewalCount,
                DaysRemaining = (uc.Status == "Active" || uc.Status == "ExpiringSoon" || uc.Status == "Renewed")
                    ? Math.Max(0, (uc.SubscriptionEndDate - DateTime.UtcNow).Days)
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

    [HttpPost("renew")]
    public async Task<IActionResult> InitiateRenewal([FromBody] RenewalRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var subscription = await _context.UserCourses
            .FirstOrDefaultAsync(uc => uc.UserId == userId 
                && uc.CourseId == request.CourseId 
                && uc.LevelId == request.LevelId);

        if (subscription == null)
            return NotFound(new { message = "Subscription not found" });

        return Ok(new { 
            message = "Renewal initiated", 
            redirectUrl = "/api/payment/initialize",
            courseId = request.CourseId,
            levelId = request.LevelId,
            isRenewal = true
        });
    }

    [HttpGet("renewal-status/{courseId}/{levelId}")]
    public async Task<IActionResult> GetRenewalStatus(int courseId, int levelId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var subscription = await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .FirstOrDefaultAsync(uc =>
                uc.UserId == userId && uc.CourseId == courseId && uc.LevelId == levelId
            );

        if (subscription == null)
            return NotFound(new { message = "Subscription not found" });

        var daysRemaining = (subscription.SubscriptionEndDate - DateTime.UtcNow).Days;
        var canRenew =
            subscription.Status == "ExpiringSoon"
            || subscription.Status == "Expired"
            || daysRemaining <= 30;

        return Ok(
            new
            {
                subscription.Status,
                subscription.SubscriptionEndDate,
                DaysRemaining = Math.Max(0, daysRemaining),
                CanRenew = canRenew,
                CourseName = subscription.Course.Name,
                LevelName = subscription.Level.Name,
            }
        );
    }
}

public class RenewalRequest
{
    public int CourseId { get; set; }
    public int LevelId { get; set; }
}
