using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        var metrics = await _analyticsService.GetDashboardMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null
    )
    {
        var analytics = await _analyticsService.GetRevenueAnalyticsAsync(startDate, endDate);
        return Ok(analytics);
    }

    [HttpGet("engagement")]
    public async Task<IActionResult> GetUserEngagementMetrics()
    {
        var metrics = await _analyticsService.GetUserEngagementMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCoursePerformance()
    {
        var performance = await _analyticsService.GetCoursePerformanceAsync();
        return Ok(performance);
    }

    [HttpGet("tests")]
    public async Task<IActionResult> GetTestAnalytics()
    {
        var analytics = await _analyticsService.GetTestAnalyticsAsync();
        return Ok(analytics);
    }

    [HttpGet("top-students")]
    public async Task<IActionResult> GetTopPerformingStudents([FromQuery] int count = 10)
    {
        var students = await _analyticsService.GetTopPerformingStudentsAsync(count);
        return Ok(students);
    }
}
