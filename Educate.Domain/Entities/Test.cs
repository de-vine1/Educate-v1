using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class Test
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int Duration { get; set; } // in minutes
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;
}

public class TestResult
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public Guid TestId { get; set; }

    public int Score { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("TestId")]
    public virtual Test Test { get; set; } = null!;
}
