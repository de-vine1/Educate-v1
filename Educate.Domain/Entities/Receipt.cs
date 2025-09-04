using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Educate.Domain.Entities;

public class Receipt
{
    [Key]
    public int ReceiptId { get; set; }

    [Required]
    public int PaymentId { get; set; }

    [Required]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PaymentId")]
    public virtual Payment Payment { get; set; } = null!;
}