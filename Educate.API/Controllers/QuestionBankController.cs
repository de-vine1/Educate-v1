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
            request.CourseId,
            request.LevelId,
            request.SubjectId,
            request.QuestionText,
            request.Options,
            request.CorrectAnswer,
            request.Explanation,
            User.Identity?.Name ?? "Admin"
        );

        return Ok(new { Success = success });
    }

    [HttpPut("{questionId}")]
    public async Task<IActionResult> UpdateQuestion(
        int questionId,
        [FromBody] UpdateQuestionRequest request
    )
    {
        var success = await _questionBankService.UpdateQuestionAsync(
            questionId,
            request.QuestionText,
            request.Options,
            request.CorrectAnswer,
            request.Explanation
        );

        return Ok(new { Success = success });
    }

    [HttpPatch("{questionId}/toggle")]
    public async Task<IActionResult> ToggleQuestionStatus(int questionId)
    {
        var success = await _questionBankService.ToggleQuestionStatusAsync(questionId);
        return Ok(new { Success = success });
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] int? courseId = null,
        [FromQuery] int? levelId = null,
        [FromQuery] int? subjectId = null
    )
    {
        var questions = await _questionBankService.GetQuestionsAsync(courseId, levelId, subjectId);
        return Ok(questions);
    }
}

public class AddQuestionRequest
{
    public int CourseId { get; set; }
    public int LevelId { get; set; }
    public int SubjectId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class UpdateQuestionRequest
{
    public string QuestionText { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}
