using Educate.Domain.Enums;

namespace Educate.Domain.Entities;

public class StudentExam
{
    public Guid Id { get; private set; }
    public Guid ExamId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public ExamStatus Status { get; private set; }
    public int? TotalScore { get; private set; }
    public bool? IsPassed { get; private set; }
    public DateTime? SubmittedAt { get; private set; }

    public Exam Exam { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();

    private StudentExam() { } // EF Core

    public static StudentExam Create(Guid examId, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID is required");

        return new StudentExam
        {
            Id = Guid.NewGuid(),
            ExamId = examId,
            UserId = userId,
            StartTime = DateTime.UtcNow,
            Status = ExamStatus.InProgress
        };
    }

    public void Submit(int totalScore, int passingScore)
    {
        if (Status != ExamStatus.InProgress)
            throw new InvalidOperationException("Can only submit an in-progress exam");

        TotalScore = totalScore;
        IsPassed = totalScore >= passingScore;
        Status = ExamStatus.Completed;
        EndTime = DateTime.UtcNow;
        SubmittedAt = DateTime.UtcNow;
    }

    public void Abandon()
    {
        if (Status != ExamStatus.InProgress)
            throw new InvalidOperationException("Can only abandon an in-progress exam");

        Status = ExamStatus.Abandoned;
        EndTime = DateTime.UtcNow;
    }
}
