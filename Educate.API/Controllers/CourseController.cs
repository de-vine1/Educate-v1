using System.Security.Claims;
using Educate.Application.Common.Models;
using Educate.Application.Courses.Commands.CreateCourse;
using Educate.Application.Courses.Queries.GetAllCourses;
using Educate.Application.Courses.Queries.GetCourseById;
using Educate.Application.Courses.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly IMediator _mediator;

    public CourseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new course
    /// </summary>
    /// <param name="command">The course creation command</param>
    /// <returns>The ID of the created course</returns>
    /// <response code="201">Returns the newly created course ID</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseCommand command)
    {
        var courseId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCourseById), new { id = courseId }, new { id = courseId });
    }

    /// <summary>
    /// Retrieves a specific course by ID
    /// </summary>
    /// <param name="id">The unique identifier of the course</param>
    /// <returns>The requested course details</returns>
    /// <response code="200">Returns the course details</response>
    /// <response code="404">If the course is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseById(Guid id)
    {
        var course = await _mediator.Send(new GetCourseByIdQuery(id));
        if (course == null)
            return NotFound();

            return Ok(course);
    }

    /// <summary>
    /// Retrieves a paginated list of all active courses
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <returns>A paginated list of courses</returns>
    /// <response code="200">Returns the paginated courses</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<CourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var courses = await _mediator.Send(new GetAllCoursesQuery(page, pageSize));
        return Ok(courses);
    }
}
