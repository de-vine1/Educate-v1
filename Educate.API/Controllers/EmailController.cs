using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    /// <summary>
    /// Sends a general email
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        await _emailService.SendEmailAsync(request.ToEmail, request.Subject, request.Message);
        return Ok(new { message = "Email sent successfully" });
    }

    /// <summary>
    /// Sends a welcome email to a new user
    /// </summary>
    [HttpPost("welcome/{email}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendWelcomeEmail(
        string email,
        [FromBody] WelcomeRequest request
    )
    {
        await _emailService.SendWelcomeEmailAsync(email, request.UserName);
        return Ok(new { message = "Welcome email sent successfully" });
    }
}

public record EmailRequest(string ToEmail, string Subject, string Message);

public record WelcomeRequest(string UserName);
