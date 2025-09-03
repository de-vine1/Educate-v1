using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class UserCourse
{
    [Key]
    public Guid UserCourseId { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public Guid LevelId { get; set; }

    public DateTime SubscriptionStartDate { get; set; } = DateTime.UtcNow;
    public DateTime SubscriptionEndDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Expired, Cancelled

    public Guid PaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    [ForeignKey("LevelId")]
    public virtual Level Level { get; set; } = null!;

    [ForeignKey("PaymentId")]
    public virtual Payment Payment { get; set; } = null!;
}
