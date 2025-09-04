using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class SubscriptionHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SubscriptionId { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public Guid LevelId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty; // Created, Renewed, Expired

    [MaxLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PaymentProvider { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PreviousEndDate { get; set; }
    public DateTime NewEndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("SubscriptionId")]
    public virtual UserCourse Subscription { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    [ForeignKey("LevelId")]
    public virtual Level Level { get; set; } = null!;
}
