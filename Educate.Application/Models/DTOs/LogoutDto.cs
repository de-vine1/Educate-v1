using System.ComponentModel.DataAnnotations;

namespace Educate.Application.Models.DTOs;

public class LogoutDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
