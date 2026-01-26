namespace Educate.Domain.Entities;

public class StudentAnswer
{
    public Guid Id { get; private set; }
    public Guid StudentExamId { get; private set; }
    public Guid QuestionId { get; private set; }
    public string AnswerText { get; private set; } = string.Empty;
    public bool? IsCorrect { get; private set; } // Null for theory questions
    public int PointsEarned { get; private set; }
    public DateTime AnsweredAt { get; private set; }

    public StudentExam StudentExam { get; set; } = null!;
    public Question Question { get; set; } = null!;

    private StudentAnswer() { } // EF Core

    public static StudentAnswer Create(
        Guid studentExamId,
        Guid questionId,
        string answerText)
    {
        if (string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Answer text is required");

        return new StudentAnswer
        {
            Id = Guid.NewGuid(),
            StudentExamId = studentExamId,
            QuestionId = questionId,
            AnswerText = answerText,
            AnsweredAt = DateTime.UtcNow,
            PointsEarned = 0
        };
    }

    public void Grade(bool isCorrect, int points)
    {
        IsCorrect = isCorrect;
        PointsEarned = isCorrect ? points : 0;
    }

    public void SetPointsEarned(int points)
    {
        if (points < 0)
            throw new ArgumentException("Points cannot be negative");

        PointsEarned = points;
    }
}
