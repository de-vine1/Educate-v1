using System.Security.Claims;
using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class BulkUploadController : ControllerBase
{
    private readonly IBulkUploadService _bulkUploadService;

    public BulkUploadController(IBulkUploadService bulkUploadService)
    {
        _bulkUploadService = bulkUploadService;
    }

    [HttpPost("courses")]
    public async Task<IActionResult> UploadCourses(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        using var stream = file.OpenReadStream();
        var result = await _bulkUploadService.UploadCoursesAsync(stream, file.FileName, adminId);
        return Ok(result);
    }

    [HttpPost("students")]
    public async Task<IActionResult> UploadStudents(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        using var stream = file.OpenReadStream();
        var result = await _bulkUploadService.UploadStudentsAsync(stream, file.FileName, adminId);
        return Ok(result);
    }

    [HttpPost("questions")]
    public async Task<IActionResult> UploadQuestions(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        using var stream = file.OpenReadStream();
        var result = await _bulkUploadService.UploadQuestionsAsync(stream, file.FileName, adminId);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetUploadHistory()
    {
        var history = await _bulkUploadService.GetUploadHistoryAsync();
        return Ok(history);
    }
}
