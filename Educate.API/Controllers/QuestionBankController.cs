using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class QuestionBankController : ControllerBase
{
    private readonly IQuestionBankService _questionBankService;

    public QuestionBankController(IQuestionBankService questionBankService)
    {
        _questionBankService = questionBankService;
    }

    [HttpPost]
    public async Task<IActionResult> AddQuestion([FromBody] AddQuestionRequest request)
    {
        var success = await _questionBankService.AddQuestionAsync(
            request.CourseId, request.LevelId, request.SubjectId,
            request.QuestionText, request.Options, request.CorrectAnswer,
            request.Explanation, request.Difficulty, User.Identity?.Name ?? "Admin");

        return Ok(new { Success = success });
    }

    [HttpPut("{questionId}")]
    public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromBody] UpdateQuestionRequest request)
    {
        var success = await _questionBankService.UpdateQuestionAsync(
            questionId, request.QuestionText, request.Options,
            request.CorrectAnswer, request.Explanation, request.Difficulty);

        return Ok(new { Success = success });
    }

    [HttpPatch("{questionId}/toggle")]
    public async Task<IActionResult> ToggleQuestionStatus(Guid questionId)
    {
        var success = await _questionBankService.ToggleQuestionStatusAsync(questionId);
        return Ok(new { Success = success });
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions([FromQuery] Guid? courseId = null,
        [FromQuery] Guid? levelId = null, [FromQuery] Guid? subjectId = null,
        [FromQuery] string? difficulty = null)
    {
        var questions = await _questionBankService.GetQuestionsAsync(courseId, levelId, subjectId, difficulty);
        return Ok(questions);
    }
}

public class AddQuestionRequest
{
    public Guid CourseId { get; set; }
    public Guid LevelId { get; set; }
    public Guid SubjectId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium";
}

public class UpdateQuestionRequest
{
    public string QuestionText { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium";
}