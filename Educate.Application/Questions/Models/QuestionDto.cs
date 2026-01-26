using Educate.Domain.Enums;

namespace Educate.Application.Questions.Models;

public record QuestionDto(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    string QuestionText,
    QuestionType QuestionType,
    string? Options,
    string? CorrectAnswer,
    int Points,
    DifficultyLevel DifficultyLevel,
    bool IsActive
);
