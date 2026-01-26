using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMediator _mediator;

    public AdminController(AppDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new course (Admin only)
    /// </summary>
    /// <param name="command">Course creation details</param>
    /// <returns>Created course ID</returns>
    [HttpPost("courses")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCourse([FromBody] Educate.Application.Courses.Commands.CreateCourse.CreateCourseCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCourse), new { id = id }, new { id = id });
    }

    /// <summary>
    /// Creates a new level for a course
    /// </summary>
    [HttpPost("levels")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateLevel([FromBody] Educate.Application.Courses.Commands.CreateLevel.CreateLevelCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    /// <summary>
    /// Creates a new subject for a level
    /// </summary>
    [HttpPost("subjects")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSubject([FromBody] Educate.Application.Courses.Commands.CreateSubject.CreateSubjectCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    /// <summary>
    /// Retrieves a course by ID
    /// </summary>
    [HttpGet("courses/{id}")]
    [ProducesResponseType(typeof(Course), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourse(Guid id)
    {
        var course = await _context.Courses.FindAsync(id);
        return course == null ? NotFound() : Ok(course);
    }

    /// <summary>
    /// Retrieves all courses (Administrative view)
    /// </summary>
    [HttpGet("courses")]
    [ProducesResponseType(typeof(List<Course>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var courses = await _context.Courses
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        // Note: For full consistency we should ideally use PaginatedResult here too, 
        // but for now we just paginate the collection return to satisfy requirements.
        return Ok(courses);
    }

    /// <summary>
    /// Updates a course details
    /// </summary>
    [HttpPut("courses/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCourse(Guid id, [FromBody] Course course)
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
            .Subscriptions.GroupBy(s => s.Level.Course.Name)
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
        // Assuming Test still links to Course directly for now. 
        // If Test links to Course, this is fine. If Test needs update, we might fail runtime later if data inconsistent.
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

    /// <summary>
    /// Retrieves payment history (Admin)
    /// </summary>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _context.Payments
            .Include(p => p.User)
            .Include(p => p.Level)
            .ThenInclude(l => l.Course)
            .AsNoTracking();

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                UserName = p.User.FirstName + " " + p.User.LastName,
                CourseName = p.Level.Course.Name,
                LevelName = p.Level.Name,
                p.Amount,
                p.PaymentMethod,
                p.Status,
                p.CreatedAt,
            })
            .ToListAsync();

        return Ok(payments);
    }

    // Question Management
    
    /// <summary>
    /// Creates a new question in the question bank
    /// </summary>
    [HttpPost("questions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateQuestion([FromBody] Educate.Application.Questions.Commands.CreateQuestion.CreateQuestionCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    /// <summary>
    /// Retrieves questions based on filters with pagination
    /// </summary>
    /// <param name="subjectId">Filter by subject ID</param>
    /// <param name="questionType">Filter by question type</param>
    /// <param name="difficultyLevel">Filter by difficulty</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    [HttpGet("questions")]
    [ProducesResponseType(typeof(Educate.Application.Common.Models.PaginatedResult<Educate.Application.Questions.Models.QuestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] Guid? subjectId, 
        [FromQuery] Educate.Domain.Enums.QuestionType? questionType, 
        [FromQuery] Educate.Domain.Enums.DifficultyLevel? difficultyLevel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new Educate.Application.Questions.Queries.GetQuestions.GetQuestionsQuery(
            subjectId, 
            questionType, 
            difficultyLevel, 
            true, 
            page, 
            pageSize);
            
        var questions = await _mediator.Send(query);
        return Ok(questions);
    }

    // Exam Management
    [HttpPost("exams")]
    public async Task<IActionResult> CreateExam([FromBody] Educate.Application.Exams.Commands.CreateExam.CreateExamCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }
}
