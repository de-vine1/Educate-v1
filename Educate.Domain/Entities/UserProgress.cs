using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class UserProgress
{
    [Key]
    public int ProgressId { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int CourseId { get; set; }

    [Required]
    public int LevelId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    [Required]
    [MaxLength(20)]
    public string CompletionStatus { get; set; } = "Not Started"; // Not Started, In Progress, Completed

    public decimal? Score { get; set; }
    public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    [ForeignKey("LevelId")]
    public virtual Level Level { get; set; } = null!;

    [ForeignKey("SubjectId")]
    public virtual Subject Subject { get; set; } = null!;
}
