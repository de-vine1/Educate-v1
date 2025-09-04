using System.ComponentModel.DataAnnotations;
using Educate.Domain.Enums;

namespace Educate.Domain.Entities;

public class BulkUploadLog
{
    [Key]
    public int UploadId { get; set; }

    public string AdminId { get; set; } = string.Empty;
    public User Admin { get; set; } = null!;

    [Required]
    public UploadType UploadType { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }

    public string ErrorLog { get; set; } = string.Empty; // JSON array of errors

    [Required]
    public UploadStatus Status { get; set; } = UploadStatus.Processing;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
