using System.Security.Claims;
using Educate.Application.Students.Queries.GetStudentDashboard;
using Educate.Infrastructure.Database;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "StudentOnly")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class StudentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMediator _mediator;

    public StudentController(AppDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves the student's dashboard summary
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var dashboard = await _mediator.Send(new GetStudentDashboardQuery(userId));
        return Ok(dashboard);
    }

    /// <summary>
    /// Retrieves the student's profile details
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Retrieves the student's active subscriptions with pagination
    /// </summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var query = _context.Subscriptions
            .Include(s => s.Level)
            .ThenInclude(l => l.Course)
            .Where(s => s.UserId == userId && s.IsActive)
            .AsNoTracking();

        var subscriptions = await query
            .OrderByDescending(s => s.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                CourseName = s.Level.Course.Name,
                LevelName = s.Level.Name,
                s.StartDate,
                s.EndDate,
                s.IsActive,
            })
            .ToListAsync();

        return Ok(subscriptions);
    }

    /// <summary>
    /// Retrieves practice materials for a specific course with pagination
    /// </summary>
    [HttpGet("materials/{courseId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaterials(Guid courseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Check if user has subscription to ANY level in this course
        var hasSubscription = await _context.Subscriptions.AnyAsync(s =>
            s.UserId == userId && s.Level.CourseId == courseId && s.IsActive
        );

        var materials = await _context
            .PracticeMaterials.Where(m => m.CourseId == courseId && (m.IsFree || hasSubscription))
            .OrderBy(m => m.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

    /// <summary>
    /// Retrieves the student's test results with pagination
    /// </summary>
    [HttpGet("test-results")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTestResults([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var query = _context
            .TestResults.Include(r => r.Test)
            .ThenInclude(t => t.Course)
            .Where(r => r.UserId == userId)
            .AsNoTracking();

        var results = await query
            .OrderByDescending(r => r.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

    // Exam Taking
    
    /// <summary>
    /// Starts a new exam attempt
    /// </summary>
    [HttpPost("exams/{examId}/start")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> StartExam(Guid examId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var attempt = await _mediator.Send(new Educate.Application.Exams.Commands.StartExam.StartExamCommand(examId, userId));
        return Ok(attempt);
    }

    /// <summary>
    /// Submits an answer for an active exam
    /// </summary>
    [HttpPost("exams/answer")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitAnswer([FromBody] Educate.Application.Exams.Commands.SubmitAnswer.SubmitAnswerCommand command)
    {
        var wasGraded = await _mediator.Send(command);
        return Ok(new { wasGraded });
    }

    /// <summary>
    /// Submits the final exam attempt
    /// </summary>
    [HttpPost("exams/{attemptId}/submit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitExam(Guid attemptId)
    {
        var result = await _mediator.Send(new Educate.Application.Exams.Commands.SubmitExam.SubmitExamCommand(attemptId));
        return Ok(result);
    }
}
