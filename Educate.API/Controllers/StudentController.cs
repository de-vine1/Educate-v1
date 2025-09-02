using System.Security.Claims;
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

    public StudentController(AppDbContext context)
    {
        _context = context;
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
        var subscriptions = await _context
            .Subscriptions.Include(s => s.Course)
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => new
            {
                s.Id,
                CourseName = s.Course.Name,
                s.StartDate,
                s.EndDate,
                s.IsActive,
            })
            .ToListAsync();

        return Ok(subscriptions);
    }

    [HttpGet("materials/{courseId}")]
    public async Task<IActionResult> GetMaterials(int courseId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var hasSubscription = await _context.Subscriptions.AnyAsync(s =>
            s.UserId == userId && s.CourseId == courseId && s.IsActive
        );

        var materials = await _context
            .PracticeMaterials.Where(m => m.CourseId == courseId && (m.IsFree || hasSubscription))
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.Content,
                m.IsFree,
            })
            .ToListAsync();

        return Ok(materials);
    }

    [HttpGet("test-results")]
    public async Task<IActionResult> GetTestResults()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var results = await _context
            .TestResults.Include(r => r.Test)
            .ThenInclude(t => t.Course)
            .Where(r => r.UserId == userId)
            .Select(r => new
            {
                r.Id,
                TestTitle = r.Test.Title,
                CourseName = r.Test.Course.Name,
                r.Score,
                r.CompletedAt,
            })
            .ToListAsync();

        return Ok(results);
    }
}
