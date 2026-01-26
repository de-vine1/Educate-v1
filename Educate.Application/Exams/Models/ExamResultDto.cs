namespace Educate.Application.Exams.Models;

public record ExamResultDto(
    Guid StudentExamId,
    string ExamTitle,
    int TotalScore,
    int TotalPoints,
    bool IsPassed,
    DateTime StartTime,
    DateTime EndTime,
    int QuestionsAnswered,
    int QuestionsTotal
);
