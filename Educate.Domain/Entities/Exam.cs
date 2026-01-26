namespace Educate.Domain.Entities;

public class Exam
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public int Duration { get; private set; } // Minutes
    public int TotalPoints { get; private set; }
    public int PassingScore { get; private set; }
    public bool IsActive { get; private set; } = false;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Subject Subject { get; set; } = null!;
    public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();

    private Exam() { } // EF Core

    public static Exam Create(
        string title,
        Guid subjectId,
        int duration,
        int passingScore)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required");

        if (duration <= 0)
            throw new ArgumentException("Duration must be greater than zero");

        if (passingScore < 0)
            throw new ArgumentException("Passing score cannot be negative");

        return new Exam
        {
            Id = Guid.NewGuid(),
            Title = title,
            SubjectId = subjectId,
            Duration = duration,
            PassingScore = passingScore,
            TotalPoints = 0, // Will be calculated when questions are added
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetTotalPoints(int totalPoints)
    {
        if (totalPoints < 0)
            throw new ArgumentException("Total points cannot be negative");

        TotalPoints = totalPoints;
    }

    public void Publish()
    {
        if (TotalPoints == 0)
            throw new InvalidOperationException("Cannot publish exam without questions");

        if (PassingScore > TotalPoints)
            throw new InvalidOperationException("Passing score cannot exceed total points");

        IsActive = true;
    }

    public void Unpublish()
    {
        IsActive = false;
    }
}
