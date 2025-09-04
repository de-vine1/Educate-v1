using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TestController : ControllerBase
{
    private readonly ITestService _testService;
    private readonly ISubscriptionService _subscriptionService;

    public TestController(ITestService testService, ISubscriptionService subscriptionService)
    {
        _testService = testService;
        _subscriptionService = subscriptionService;
    }

    [HttpPost("practice/start")]
    public async Task<IActionResult> StartPracticeTest([FromBody] StartPracticeTestRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var hasAccess = await _subscriptionService.HasActiveSubscriptionAsync(userId, request.SubjectId);
        if (!hasAccess)
            return Forbid("Active subscription required");

        var result = await _testService.StartPracticeTestAsync(userId, request.SubjectId, request.QuestionCount);
        return Ok(result);
    }

    [HttpPost("mock/start")]
    public async Task<IActionResult> StartMockExam([FromBody] StartMockExamRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var hasAccess = await _subscriptionService.HasActiveSubscriptionForLevelAsync(userId, request.CourseId, request.LevelId);
        if (!hasAccess)
            return Forbid("Active subscription required");

        var result = await _testService.StartMockExamAsync(userId, request.CourseId, request.LevelId);
        return Ok(result);
    }

    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetTestSession(Guid sessionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _testService.GetTestSessionAsync(userId, sessionId);
        return Ok(result);
    }

    [HttpPost("session/{sessionId}/answer")]
    public async Task<IActionResult> SubmitAnswer(Guid sessionId, [FromBody] SubmitAnswerRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _testService.SubmitAnswerAsync(userId, sessionId, request.QuestionId, request.Answer);
        return Ok(new { Success = success });
    }

    [HttpPost("session/{sessionId}/complete")]
    public async Task<IActionResult> CompleteTest(Guid sessionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _testService.CompleteTestAsync(userId, sessionId);
        return Ok(result);
    }

    [HttpGet("results/{attemptId}")]
    public async Task<IActionResult> GetTestResults(Guid attemptId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _testService.GetTestResultsAsync(userId, attemptId);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetTestHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _testService.GetUserTestHistoryAsync(userId);
        return Ok(result);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetTestAnalytics()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _testService.GetTestAnalyticsAsync(userId);
        return Ok(result);
    }
}

public class StartPracticeTestRequest
{
    public Guid SubjectId { get; set; }
    public int QuestionCount { get; set; } = 10;
}

public class StartMockExamRequest
{
    public Guid CourseId { get; set; }
    public Guid LevelId { get; set; }
}

public class SubmitAnswerRequest
{
    public Guid QuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
}