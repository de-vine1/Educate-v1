using Educate.Domain.Enums;

namespace Educate.Application.Exams.Models;

public record ExamAttemptDto(
    Guid AttemptId,
    Guid ExamId,
    string ExamTitle,
    int Duration,
    DateTime StartTime,
    List<ExamQuestionDto> Questions
);

public record ExamQuestionDto(
    Guid QuestionId,
    string QuestionText,
    QuestionType QuestionType,
    string? Options,
    int Points,
    int OrderNumber
);
