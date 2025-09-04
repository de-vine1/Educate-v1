using System.ComponentModel.DataAnnotations;

namespace Educate.Domain.Entities;

public class AdminAlert
{
    [Key]
    public Guid AlertId { get; set; } = Guid.NewGuid();

    [Required]
    public string AlertType { get; set; } = string.Empty; // PaymentFailure, SubscriptionExpiry, BulkUploadFailed

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }

    public bool IsRead { get; set; } = false;
    public bool IsResolved { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
