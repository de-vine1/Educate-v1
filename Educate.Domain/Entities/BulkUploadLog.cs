using System.ComponentModel.DataAnnotations;

namespace Educate.Domain.Entities;

public class BulkUploadLog
{
    [Key]
    public Guid UploadId { get; set; } = Guid.NewGuid();

    public string AdminId { get; set; } = string.Empty;
    public User Admin { get; set; } = null!;

    [Required]
    public string UploadType { get; set; } = string.Empty; // Courses, Students, Questions

    [Required]
    public string FileName { get; set; } = string.Empty;

    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }

    public string ErrorLog { get; set; } = string.Empty; // JSON array of errors

    [Required]
    public string Status { get; set; } = "Processing"; // Processing, Completed, Failed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}