using System.Security.Claims;
using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "StudentOnly")]
public class StudentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;

    public StudentController(AppDbContext context, ISubscriptionService subscriptionService)
    {
        _context = context;
        _subscriptionService = subscriptionService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        return Ok(
            new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.StudentId,
                user.SubscriptionStatus,
            }
        );
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subscriptions = await _subscriptionService.GetUserActiveSubscriptionsAsync(userId);
        return Ok(subscriptions);
    }

    [HttpGet("materials/{courseId}/{levelId}")]
    public async Task<IActionResult> GetMaterials(Guid courseId, Guid levelId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var hasSubscription = await _subscriptionService.HasActiveSubscriptionAsync(
            userId,
            courseId,
            levelId
        );

        var materials = await _context
            .PracticeMaterials.Where(m =>
                m.CourseId == courseId && m.LevelId == levelId && (m.IsFree || hasSubscription)
            )
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.Content,
                m.IsFree,
                HasAccess = m.IsFree || hasSubscription,
            })
            .ToListAsync();

        return Ok(materials);
    }

    [HttpGet("test-results")]
    public async Task<IActionResult> GetTestResults()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var results = await _context
            .Tests.Include(t => t.Course)
            .Select(t => new
            {
                t.Id,
                TestTitle = t.Title,
                CourseName = t.Course.Name,
                Duration = t.Duration,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync();

        return Ok(results);
    }
}
