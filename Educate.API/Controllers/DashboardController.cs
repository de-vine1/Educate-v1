using System.Security.Claims;
using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ISubscriptionService _subscriptionService;

    public DashboardController(
        IDashboardService dashboardService,
        ISubscriptionService subscriptionService
    )
    {
        _dashboardService = dashboardService;
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var dashboardData = await _dashboardService.GetDashboardDataAsync(userId);
        return Ok(dashboardData);
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var subscriptions = await _dashboardService.GetUserSubscriptionsAsync(userId);
        return Ok(subscriptions);
    }

    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var progress = await _dashboardService.GetProgressSummaryAsync(userId);
        return Ok(progress);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPaymentHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var payments = await _dashboardService.GetPaymentHistoryAsync(userId);
        return Ok(payments);
    }

    [HttpPost("progress")]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var success = await _dashboardService.UpdateProgressAsync(
            userId,
            request.SubjectId,
            request.Status,
            request.Score
        );
        if (!success)
            return BadRequest("Failed to update progress");

        return Ok(new { message = "Progress updated successfully" });
    }

    [HttpGet("access-check/{courseId}/{levelId}")]
    public async Task<IActionResult> CheckAccess(Guid courseId, Guid levelId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var hasAccess = await _subscriptionService.HasActiveSubscriptionAsync(
            userId,
            courseId,
            levelId
        );
        return Ok(new { hasAccess });
    }
}

public class UpdateProgressRequest
{
    public Guid SubjectId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }
}
