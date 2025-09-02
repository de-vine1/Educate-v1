using System.ComponentModel.DataAnnotations;

namespace Educate.Application.Models.DTOs;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}
