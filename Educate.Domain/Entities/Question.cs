using Educate.Domain.Enums;

namespace Educate.Domain.Entities;

public class Question
{
    public Guid Id { get; private set; }
    public Guid SubjectId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public QuestionType QuestionType { get; private set; }
    public string? Options { get; private set; } // JSON array for MCQ options
    public string? CorrectAnswer { get; private set; } // "A", "B", "C", etc for MCQ
    public int Points { get; private set; } = 1;
    public DifficultyLevel DifficultyLevel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Subject Subject { get; set; } = null!;

    private Question() { } // EF Core

    public static Question Create(
        Guid subjectId,
        string questionText,
        QuestionType questionType,
        DifficultyLevel difficultyLevel,
        int points = 1,
        string? options = null,
        string? correctAnswer = null)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text is required");

        if (questionType == QuestionType.MultipleChoice && string.IsNullOrEmpty(options))
            throw new ArgumentException("Options are required for multiple choice questions");

        if (questionType == QuestionType.MultipleChoice && string.IsNullOrEmpty(correctAnswer))
            throw new ArgumentException("Correct answer is required for multiple choice questions");

        if (points <= 0)
            throw new ArgumentException("Points must be greater than zero");

        return new Question
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            QuestionText = questionText,
            QuestionType = questionType,
            Options = options,
            CorrectAnswer = correctAnswer,
            Points = points,
            DifficultyLevel = difficultyLevel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Update(string questionText, string? options, string? correctAnswer, int points, DifficultyLevel difficultyLevel)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text is required");

        if (points <= 0)
            throw new ArgumentException("Points must be greater than zero");

        QuestionText = questionText;
        Options = options;
        CorrectAnswer = correctAnswer;
        Points = points;
        DifficultyLevel = difficultyLevel;
    }
}
