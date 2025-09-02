using System.Security.Claims;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly AppDbContext _context;

    public CourseController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _context
            .Courses.Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.AnnualPrice,
            })
            .ToListAsync();

        return Ok(courses);
    }

    [HttpPost("{courseId}/subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe(int courseId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var existingSubscription = await _context.Subscriptions.AnyAsync(s =>
            s.UserId == userId && s.CourseId == courseId && s.IsActive
        );

        if (existingSubscription)
            return BadRequest("Already subscribed to this course");

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
            return NotFound();

        var subscription = new Subscription
        {
            UserId = userId!,
            CourseId = courseId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            AmountPaid = course.AnnualPrice,
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Subscription created successfully" });
    }
}
