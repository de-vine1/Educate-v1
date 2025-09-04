using System.ComponentModel.DataAnnotations;

namespace Educate.Domain.Entities;

public class TestSession
{
    [Key]
    public int SessionId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int LevelId { get; set; }
    public Level Level { get; set; } = null!;

    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }

    [Required]
    public string TestType { get; set; } = string.Empty; // Practice, Mock

    public string Questions { get; set; } = string.Empty; // JSON array of question IDs

    public string CurrentAnswers { get; set; } = string.Empty; // JSON: {"questionId": "selectedAnswer"}

    public int CurrentQuestionIndex { get; set; } = 0;

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public int TimeLimit { get; set; } = 0; // Minutes, 0 = no limit

    public bool IsActive { get; set; } = true;
    public bool IsCompleted { get; set; } = false;
}
