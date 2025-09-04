using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class StudyMaterial
{
    [Key]
    public Guid MaterialId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SubjectId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string MaterialType { get; set; } = string.Empty; // PDF, Video, Audio, Document

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public bool IsDownloadable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("SubjectId")]
    public virtual Subject Subject { get; set; } = null!;
}
