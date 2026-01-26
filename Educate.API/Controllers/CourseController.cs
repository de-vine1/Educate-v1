using System.Security.Claims;
using Educate.Application.Courses.Commands.CreateCourse;
using Educate.Application.Courses.Commands.SubscribeToCourse;
using Educate.Application.Courses.Queries.GetAllCourses;
using Educate.Application.Courses.Queries.GetCourseById;
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

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseCommand command)
    {
        var courseId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCourseById), new { id = courseId }, new { id = courseId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourseById(int id)
    {
        var course = await _mediator.Send(new GetCourseByIdQuery(id));
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _mediator.Send(new GetAllCoursesQuery());
        return Ok(courses);
    }

    [HttpPost("{courseId}/subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe(int courseId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try 
        {
            var result = await _mediator.Send(new SubscribeToCourseCommand(courseId, userId));
            if (!result)
                return BadRequest("Already subscribed or course not found");

            return Ok(new { message = "Subscription created successfully" });
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }
}
