using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminAlertController : ControllerBase
{
    private readonly IAdminAlertService _adminAlertService;

    public AdminAlertController(IAdminAlertService adminAlertService)
    {
        _adminAlertService = adminAlertService;
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadAlerts()
    {
        var alerts = await _adminAlertService.GetUnreadAlertsAsync();
        return Ok(alerts);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAlerts([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var alerts = await _adminAlertService.GetAllAlertsAsync(page, pageSize);
        return Ok(alerts);
    }

    [HttpPatch("{alertId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid alertId)
    {
        var success = await _adminAlertService.MarkAsReadAsync(alertId);
        return Ok(new { Success = success });
    }

    [HttpPatch("{alertId}/resolve")]
    public async Task<IActionResult> MarkAsResolved(Guid alertId)
    {
        var success = await _adminAlertService.MarkAsResolvedAsync(alertId);
        return Ok(new { Success = success });
    }
}