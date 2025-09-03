using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class Payment
{
    [Key]
    public Guid PaymentId { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;

    public Guid? CourseId { get; set; }
    public Guid? LevelId { get; set; }
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // Paystack, Monnify

    [Required]
    public string Reference { get; set; } = string.Empty; // Unique payment reference

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed

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
