using System.Security.Claims;
using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class StudentManagementController : ControllerBase
{
    private readonly IStudentManagementService _studentManagementService;

    public StudentManagementController(IStudentManagementService studentManagementService)
    {
        _studentManagementService = studentManagementService;
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetAllStudents(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? courseFilter = null,
        [FromQuery] string? statusFilter = null
    )
    {
        var students = await _studentManagementService.GetAllStudentsAsync(
            searchTerm,
            courseFilter,
            statusFilter
        );
        return Ok(students);
    }

    [HttpGet("students/{userId}")]
    public async Task<IActionResult> GetStudentDetails(string userId)
    {
        var details = await _studentManagementService.GetStudentDetailsAsync(userId);
        return Ok(details);
    }

    [HttpPost("students/{userId}/extend-subscription")]
    public async Task<IActionResult> ExtendSubscription(
        string userId,
        [FromBody] ExtendSubscriptionRequest request
    )
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var success = await _studentManagementService.ExtendSubscriptionAsync(
            userId,
            request.CourseId,
            request.LevelId,
            request.Months,
            adminId
        );

        return Ok(new { Success = success });
    }

    [HttpPost("students/{userId}/toggle-scholarship")]
    public async Task<IActionResult> ToggleScholarship(
        string userId,
        [FromBody] ToggleScholarshipRequest request
    )
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var success = await _studentManagementService.ToggleScholarshipAsync(
            userId,
            request.CourseId,
            request.LevelId,
            adminId
        );

        return Ok(new { Success = success });
    }

    [HttpPost("students/{userId}/reset-tests")]
    public async Task<IActionResult> ResetTestAttempts(
        string userId,
        [FromBody] ResetTestsRequest? request = null
    )
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var success = await _studentManagementService.ResetTestAttemptsAsync(
            userId,
            request?.CourseId,
            adminId
        );

        return Ok(new { Success = success });
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> SendAnnouncement([FromBody] SendAnnouncementRequest request)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await _studentManagementService.SendAnnouncementAsync(
            request.Title,
            request.Message,
            request.TargetCourse,
            request.TargetLevel,
            request.TargetUserId,
            adminId
        );

        return Ok(result);
    }
}

public class ExtendSubscriptionRequest
{
    public Guid CourseId { get; set; }
    public Guid LevelId { get; set; }
    public int Months { get; set; }
}

public class ToggleScholarshipRequest
{
    public Guid CourseId { get; set; }
    public Guid LevelId { get; set; }
}

public class ResetTestsRequest
{
    public Guid? CourseId { get; set; }
}

public class SendAnnouncementRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TargetCourse { get; set; }
    public string? TargetLevel { get; set; }
    public string? TargetUserId { get; set; }
}
