using Educate.Application.Models.DTOs;

namespace Educate.Application.Models.DTOs;

public class EnrollmentRequestDto
{
    public int CourseId { get; set; }
    public int LevelId { get; set; }
}

public class EnrollmentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? UserCourseId { get; set; }
    public int? PaymentId { get; set; }
    public string? PaymentUrl { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentCallbackDto
{
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
