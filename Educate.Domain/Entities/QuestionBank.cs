using System.ComponentModel.DataAnnotations;

namespace Educate.Domain.Entities;

public class QuestionBank
{
    [Key]
    public Guid QuestionId { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid LevelId { get; set; }
    public Level Level { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public string Options { get; set; } = string.Empty; // JSON: {"A": "Option A", "B": "Option B", ...}

    [Required]
    public string CorrectAnswer { get; set; } = string.Empty; // A, B, C, or D

    public string Explanation { get; set; } = string.Empty;

    [Required]
    public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard

    public bool IsActive { get; set; } = true;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
