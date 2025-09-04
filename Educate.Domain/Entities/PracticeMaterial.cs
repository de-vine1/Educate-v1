using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class PracticeMaterial
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public Guid LevelId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsFree { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    [ForeignKey("LevelId")]
    public virtual Level Level { get; set; } = null!;
}
