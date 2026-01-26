using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public SecurityController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Retrieves a CSRF token for secure form submissions
    /// </summary>
    [HttpGet("csrf-token")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}
