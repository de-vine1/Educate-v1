using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Educate.Domain.Enums;

namespace Educate.Domain.Entities;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public int? CourseId { get; set; }
    public int? LevelId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public PaymentProvider Provider { get; set; }

    [Required]
    public string Reference { get; set; } = string.Empty; // Unique payment reference

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CourseId")]
    public virtual Course? Course { get; set; }

    [ForeignKey("LevelId")]
    public virtual Level? Level { get; set; }

    public virtual UserCourse? UserCourse { get; set; }
}
