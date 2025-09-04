using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
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
    private readonly IAuditService _auditService;
    private readonly ISubscriptionService _subscriptionService;

    public AdminController(
        AppDbContext context,
        IAuditService auditService,
        ISubscriptionService subscriptionService
    )
    {
        _context = context;
        _auditService = auditService;
        _subscriptionService = subscriptionService;
    }

    // Course Management
    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        // Validate duplicate course name
        if (await _context.Courses.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower()))
            return BadRequest("A course with this name already exists.");

        var course = new Course { Name = dto.Name, Description = dto.Description };
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        await _auditService.LogAsync(
            userId,
            "CREATE_COURSE",
            $"Created course: {course.Name}",
            ipAddress,
            userAgent
        );

        var courseDto = new CourseDto
        {
            CourseId = course.CourseId,
            Name = course.Name,
            Description = course.Description,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt,
        };

        return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, courseDto);
    }

    [HttpGet("courses/{id}")]
    public async Task<IActionResult> GetCourse(int id)
    {
        var course = await _context
            .Courses.Include(c => c.Levels)
            .ThenInclude(l => l.Subjects)
            .FirstOrDefaultAsync(c => c.CourseId == id);

        if (course == null)
            return NotFound();

        var courseDto = new CourseDto
        {
            CourseId = course.CourseId,
            Name = course.Name,
            Description = course.Description,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt,
            Levels = course
                .Levels.OrderBy(l => l.Order)
                .Select(l => new LevelDto
                {
                    LevelId = l.LevelId,
                    CourseId = l.CourseId,
                    Name = l.Name,
                    Order = l.Order,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    Subjects = l
                        .Subjects.Select(s => new SubjectDto
                        {
                            SubjectId = s.SubjectId,
                            LevelId = s.LevelId,
                            Name = s.Name,
                            CreatedAt = s.CreatedAt,
                            UpdatedAt = s.UpdatedAt,
                        })
                        .ToList(),
                })
                .ToList(),
        };

        return Ok(courseDto);
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var totalCount = await _context.Courses.CountAsync();

        var courses = await _context
            .Courses.Include(c => c.Levels)
            .ThenInclude(l => l.Subjects)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var courseDtos = courses
            .Select(course => new CourseDto
            {
                CourseId = course.CourseId,
                Name = course.Name,
                Description = course.Description,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                Levels = course
                    .Levels.OrderBy(l => l.Order)
                    .Select(l => new LevelDto
                    {
                        LevelId = l.LevelId,
                        CourseId = l.CourseId,
                        Name = l.Name,
                        Order = l.Order,
                        CreatedAt = l.CreatedAt,
                        UpdatedAt = l.UpdatedAt,
                        Subjects = l
                            .Subjects.Select(s => new SubjectDto
                            {
                                SubjectId = s.SubjectId,
                                LevelId = s.LevelId,
                                Name = s.Name,
                                CreatedAt = s.CreatedAt,
                                UpdatedAt = s.UpdatedAt,
                            })
                            .ToList(),
                    })
                    .ToList(),
            })
            .ToList();

        var result = new PagedResult<CourseDto>
        {
            Items = courseDtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };

        return Ok(result);
    }

    [HttpPut("courses/{id}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
            return NotFound();

        // Validate duplicate course name (excluding current course)
        if (
            await _context.Courses.AnyAsync(c =>
                c.Name.ToLower() == dto.Name.ToLower() && c.CourseId != id
            )
        )
            return BadRequest("A course with this name already exists.");

        var oldName = course.Name;
        course.Name = dto.Name;
        course.Description = dto.Description;
        course.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        await _auditService.LogAsync(
            userId,
            "UPDATE_COURSE",
            $"Updated course: {oldName} -> {course.Name}",
            ipAddress,
            userAgent
        );

        return NoContent();
    }

    [HttpDelete("courses/{id}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
            return NotFound();

        var courseName = course.Name;
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        await _auditService.LogAsync(
            userId,
            "DELETE_COURSE",
            $"Deleted course: {courseName}",
            ipAddress,
            userAgent
        );

        return NoContent();
    }

    // Level Management - Nested under courses
    [HttpPost("courses/{courseId}/levels")]
    public async Task<IActionResult> CreateLevel(int courseId, [FromBody] CreateLevelDto dto)
    {
        // Ensure course exists
        if (!await _context.Courses.AnyAsync(c => c.CourseId == courseId))
            return NotFound("Course not found");

        dto.CourseId = courseId; // Override with route parameter
        return await CreateLevelInternal(dto);
    }

    [HttpPost("levels")]
    public async Task<IActionResult> CreateLevelInternal([FromBody] CreateLevelDto dto)
    {
        // Validate duplicate level name within the same course
        if (
            await _context.Levels.AnyAsync(l =>
                l.CourseId == dto.CourseId && l.Name.ToLower() == dto.Name.ToLower()
            )
        )
            return BadRequest("A level with this name already exists in this course.");

        // Validate duplicate order within the same course
        if (await _context.Levels.AnyAsync(l => l.CourseId == dto.CourseId && l.Order == dto.Order))
            return BadRequest("A level with this order already exists in this course.");

        var level = new Level
        {
            CourseId = dto.CourseId,
            Name = dto.Name,
            Order = dto.Order,
        };

        _context.Levels.Add(level);
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var course = await _context.Courses.FindAsync(dto.CourseId);
        await _auditService.LogAsync(
            userId,
            "CREATE_LEVEL",
            $"Created level: {level.Name} in course: {course?.Name}",
            ipAddress,
            userAgent
        );

        var levelDto = new LevelDto
        {
            LevelId = level.LevelId,
            CourseId = level.CourseId,
            Name = level.Name,
            Order = level.Order,
            CreatedAt = level.CreatedAt,
            UpdatedAt = level.UpdatedAt,
        };

        return CreatedAtAction(nameof(GetLevel), new { id = level.LevelId }, levelDto);
    }

    [HttpGet("levels/{id}")]
    public async Task<IActionResult> GetLevel(int id)
    {
        var level = await _context
            .Levels.Include(l => l.Subjects)
            .FirstOrDefaultAsync(l => l.LevelId == id);

        if (level == null)
            return NotFound();

        var levelDto = new LevelDto
        {
            LevelId = level.LevelId,
            CourseId = level.CourseId,
            Name = level.Name,
            Order = level.Order,
            CreatedAt = level.CreatedAt,
            UpdatedAt = level.UpdatedAt,
            Subjects = level
                .Subjects.Select(s => new SubjectDto
                {
                    SubjectId = s.SubjectId,
                    LevelId = s.LevelId,
                    Name = s.Name,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                })
                .ToList(),
        };

        return Ok(levelDto);
    }

    [HttpPut("levels/{id}")]
    public async Task<IActionResult> UpdateLevel(int id, [FromBody] UpdateLevelDto dto)
    {
        var level = await _context.Levels.FindAsync(id);
        if (level == null)
            return NotFound();

        // Validate duplicate level name within the same course (excluding current level)
        if (
            await _context.Levels.AnyAsync(l =>
                l.CourseId == level.CourseId
                && l.Name.ToLower() == dto.Name.ToLower()
                && l.LevelId != id
            )
        )
            return BadRequest("A level with this name already exists in this course.");

        // Validate duplicate order within the same course (excluding current level)
        if (
            await _context.Levels.AnyAsync(l =>
                l.CourseId == level.CourseId && l.Order == dto.Order && l.LevelId != id
            )
        )
            return BadRequest("A level with this order already exists in this course.");

        var oldName = level.Name;
        level.Name = dto.Name;
        level.Order = dto.Order;
        level.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        await _auditService.LogAsync(
            userId,
            "UPDATE_LEVEL",
            $"Updated level: {oldName} -> {level.Name}",
            ipAddress,
            userAgent
        );

        return NoContent();
    }

    [HttpDelete("levels/{id}")]
    public async Task<IActionResult> DeleteLevel(int id)
    {
        var level = await _context
            .Levels.Include(l => l.Subjects)
            .FirstOrDefaultAsync(l => l.LevelId == id);
        if (level == null)
            return NotFound();

        // Check for dependent subjects
        if (level.Subjects.Any())
            return BadRequest("Cannot delete level with existing subjects. Delete subjects first.");

        var levelName = level.Name;
        _context.Levels.Remove(level);
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        await _auditService.LogAsync(
            userId,
            "DELETE_LEVEL",
            $"Deleted level: {levelName}",
            ipAddress,
            userAgent
        );

        return NoContent();
    }

    // Subject Management - Nested under levels
    [HttpPost("levels/{levelId}/subjects")]
    public async Task<IActionResult> CreateSubject(int levelId, [FromBody] CreateSubjectDto dto)
    {
        // Ensure level exists
        if (!await _context.Levels.AnyAsync(l => l.LevelId == levelId))
            return NotFound("Level not found");

        dto.LevelId = levelId; // Override with route parameter
        return await CreateSubjectInternal(dto);
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubjectInternal([FromBody] CreateSubjectDto dto)
    {
        // Validate duplicate subject name within the same level
        if (
            await _context.Subjects.AnyAsync(s =>
                s.LevelId == dto.LevelId && s.Name.ToLower() == dto.Name.ToLower()
            )
        )
            return BadRequest("A subject with this name already exists in this level.");

        var subject = new Subject { LevelId = dto.LevelId, Name = dto.Name };
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var level = await _context
            .Levels.Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.LevelId == dto.LevelId);
        await _auditService.LogAsync(
            userId,
            "CREATE_SUBJECT",
            $"Created subject: {subject.Name} in level: {level?.Name} (Course: {level?.Course?.Name})",
            ipAddress,
            userAgent
        );

        var subjectDto = new SubjectDto
        {
            SubjectId = subject.SubjectId,
            LevelId = subject.LevelId,
            Name = subject.Name,
            CreatedAt = subject.CreatedAt,
            UpdatedAt = subject.UpdatedAt,
        };

        return CreatedAtAction(nameof(GetSubject), new { id = subject.SubjectId }, subjectDto);
    }

    [HttpGet("subjects/{id}")]
    public async Task<IActionResult> GetSubject(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
            return NotFound();

        var subjectDto = new SubjectDto
        {
            SubjectId = subject.SubjectId,
            LevelId = subject.LevelId,
            Name = subject.Name,
            CreatedAt = subject.CreatedAt,
            UpdatedAt = subject.UpdatedAt,
        };

        return Ok(subjectDto);
    }

    [HttpPut("subjects/{id}")]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectDto dto)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
            return NotFound();

        // Validate duplicate subject name within the same level (excluding current subject)
        if (
            await _context.Subjects.AnyAsync(s =>
                s.LevelId == subject.LevelId
                && s.Name.ToLower() == dto.Name.ToLower()
                && s.SubjectId != id
            )
        )
            return BadRequest("A subject with this name already exists in this level.");

        var oldName = subject.Name;
        subject.Name = dto.Name;
        subject.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        await _auditService.LogAsync(
            userId,
            "UPDATE_SUBJECT",
            $"Updated subject: {oldName} -> {subject.Name}",
            ipAddress,
            userAgent
        );

        return NoContent();
    }

    [HttpDelete("subjects/{id}")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
            return NotFound();

        // TODO: Add constraint check for exams/questions when those entities are implemented
        // if (subject.Questions.Any() || subject.Tests.Any())
        //     return BadRequest("Cannot delete subject with existing questions or tests.");

        var subjectName = subject.Name;
        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();

        // Audit log
        var userId = User.FindFirst("sub")?.Value ?? "Unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        await _auditService.LogAsync(
            userId,
            "DELETE_SUBJECT",
            $"Deleted subject: {subjectName}",
            ipAddress,
            userAgent
        );

        return NoContent();
    }

    [HttpGet("analytics/subscriptions")]
    public async Task<IActionResult> GetSubscriptionAnalytics()
    {
        var analytics = await _context
            .UserCourses.Include(uc => uc.Course)
            .GroupBy(uc => uc.Course.Name)
            .Select(g => new
            {
                CourseName = g.Key,
                ActiveSubscriptions = g.Count(uc =>
                    uc.Status == "Active" || uc.Status == "Renewed"
                ),
                TotalSubscriptions = g.Count(),
            })
            .ToListAsync();

        return Ok(analytics);
    }

    [HttpGet("analytics/engagement")]
    public async Task<IActionResult> GetEngagementAnalytics()
    {
        var engagement = await _context
            .Tests.GroupBy(t => t.Course.Name)
            .Select(g => new
            {
                CourseName = g.Key,
                TestsCreated = g.Count(),
                AverageDuration = g.Average(t => t.Duration),
            })
            .ToListAsync();

        return Ok(engagement);
    }

    // Phase 4.7: Admin Panel Controls for Subscriptions
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetAllSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50
    )
    {
        var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
        var pagedResult = subscriptions.Skip((page - 1) * pageSize).Take(pageSize);

        return Ok(
            new
            {
                items = pagedResult,
                totalCount = subscriptions.Count(),
                page,
                pageSize,
            }
        );
    }

    [HttpGet("subscriptions/user/{userId}/history")]
    public async Task<IActionResult> GetUserRenewalHistory(string userId)
    {
        var history = await _subscriptionService.GetUserRenewalHistoryAsync(userId);
        return Ok(history);
    }

    [HttpGet("reports/renewals-vs-expiries")]
    public async Task<IActionResult> GetRenewalsVsExpiriesReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate
    )
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var renewals = await _context
            .SubscriptionHistories.Where(sh =>
                sh.Action == "Renewed" && sh.CreatedAt >= start && sh.CreatedAt <= end
            )
            .GroupBy(sh => sh.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var expiries = await _context
            .SubscriptionHistories.Where(sh =>
                sh.Action == "Expired" && sh.CreatedAt >= start && sh.CreatedAt <= end
            )
            .GroupBy(sh => sh.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(
            new
            {
                renewals,
                expiries,
                startDate = start,
                endDate = end,
            }
        );
    }
}
