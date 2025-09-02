namespace Educate.Application.Models.DTOs;

public class RegisterResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public bool EmailConfirmationRequired { get; set; } = true;
}
