using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] Course course)
    {
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    [HttpGet("courses/{id}")]
    public async Task<IActionResult> GetCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        return course == null ? NotFound() : Ok(course);
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _context.Courses.ToListAsync();
        return Ok(courses);
    }

    [HttpPut("courses/{id}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] Course course)
    {
        if (id != course.Id)
            return BadRequest();

        _context.Entry(course).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("analytics/subscriptions")]
    public async Task<IActionResult> GetSubscriptionAnalytics()
    {
        var analytics = await _context
            .Subscriptions.GroupBy(s => s.Course.Name)
            .Select(g => new
            {
                CourseName = g.Key,
                ActiveSubscriptions = g.Count(s => s.IsActive),
                TotalRevenue = g.Sum(s => s.AmountPaid),
            })
            .ToListAsync();

        return Ok(analytics);
    }

    [HttpGet("analytics/engagement")]
    public async Task<IActionResult> GetEngagementAnalytics()
    {
        var engagement = await _context
            .TestResults.GroupBy(r => r.Test.Course.Name)
            .Select(g => new
            {
                CourseName = g.Key,
                TestsTaken = g.Count(),
                AverageScore = g.Average(r => r.Score),
            })
            .ToListAsync();

        return Ok(engagement);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments()
    {
        var payments = await _context
            .Payments.Include(p => p.User)
            .Include(p => p.Course)
            .Select(p => new
            {
                p.Id,
                UserName = p.User.FirstName + " " + p.User.LastName,
                CourseName = p.Course.Name,
                p.Amount,
                p.PaymentMethod,
                p.Status,
                p.CreatedAt,
            })
            .ToListAsync();

        return Ok(payments);
    }
}
