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
            .Courses.Include(c => c.Levels)
            .ThenInclude(l => l.Subjects)
            .Select(c => new
            {
                c.CourseId,
                c.Name,
                c.Description,
                c.CreatedAt,
                Levels = c
                    .Levels.OrderBy(l => l.Order)
                    .Select(l => new
                    {
                        l.LevelId,
                        l.Name,
                        l.Order,
                        SubjectCount = l.Subjects.Count(),
                    }),
            })
            .ToListAsync();

        return Ok(courses);
    }
}
