using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Educate.Domain.Enums;

namespace Educate.Domain.Entities;

public class Subscription
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int CourseId { get; set; }

    [Required]
    public int LevelId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Required]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    [ForeignKey("LevelId")]
    public virtual Level Level { get; set; } = null!;
}
