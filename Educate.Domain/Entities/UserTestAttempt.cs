using System.ComponentModel.DataAnnotations;

namespace Educate.Domain.Entities;

public class UserTestAttempt
{
    [Key]
    public int AttemptId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int LevelId { get; set; }
    public Level Level { get; set; } = null!;

    public int? SubjectId { get; set; } // Null for full mock exams
    public Subject? Subject { get; set; }

    [Required]
    public string TestType { get; set; } = string.Empty; // Practice, Mock

    public decimal Score { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }

    public string Answers { get; set; } = string.Empty; // JSON: {"questionId": "selectedAnswer"}

    public DateTime AttemptDate { get; set; } = DateTime.UtcNow;
    public TimeSpan TimeTaken { get; set; }

    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
}
