using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IEmailService _emailService;

    public TestController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-test-email")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            await _emailService.SendEmailAsync(
                request.ToEmail,
                "Test Email",
                "This is a test email from Educate API"
            );
            return Ok(new { message = "Test email sent successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record TestEmailRequest(string ToEmail);
