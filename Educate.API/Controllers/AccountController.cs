using Educate.Application.Account.Commands.ChangePassword;
using Educate.Application.Account.Commands.ConfirmEmailChange;
using Educate.Application.Account.Commands.RequestEmailChange;
using Educate.Application.Account.Commands.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Changes the current user's password
    /// </summary>
    [HttpPut("password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Updates user profile information
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new UpdateProfileCommand(userId, request.FirstName, request.LastName, request.Username);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Initiates an email change request
    /// </summary>
    [HttpPost("email/request-change")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestEmailChange([FromBody] RequestEmailChangeRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new RequestEmailChangeCommand(userId, request.NewEmail, request.Password);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Confirms and finalizes an email change
    /// </summary>
    [HttpPost("email/confirm-change")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new ConfirmEmailChangeCommand(userId, request.OldEmailCode, request.NewEmailCode);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Deletes the user account permanently
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new Educate.Application.Account.Commands.DeleteAccount.DeleteAccountCommand(userId, request.Password, request.Reason);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Retrieves all active sessions for the user
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new Educate.Application.Account.Queries.GetActiveSessions.GetActiveSessionsQuery(userId);
        var sessions = await _mediator.Send(query);

        return Ok(sessions);
    }

    /// <summary>
    /// Revokes a specific session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeSession(Guid sessionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new Educate.Application.Account.Commands.RevokeSession.RevokeSessionCommand(userId, sessionId);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Revokes all sessions except the current one
    /// </summary>
    [HttpDelete("sessions/all")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new Educate.Application.Account.Commands.RevokeAllSessions.RevokeAllSessionsCommand(userId);
        var result = await _mediator.Send(command);

        return Ok(new { message = result.Message, sessionsRevoked = result.SessionsRevoked });
    }

    // Two-Factor Authentication
    
    /// <summary>
    /// Enables 2FA for the account
    /// </summary>
    [HttpPost("2fa/enable")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Enable2FA()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new Educate.Application.Account.Commands.Enable2FA.Enable2FACommand(userId);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            message = result.Message,
            qrCodeUri = result.QrCodeUri,
            manualEntryKey = result.ManualEntryKey,
            recoveryCodes = result.RecoveryCodes
        });
    }

    /// <summary>
    /// Verifies 2FA code
    /// </summary>
    [HttpPost("2fa/verify")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new Educate.Application.Account.Commands.Verify2FA.Verify2FACommand(userId, request.Code);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Disables 2FA authentication
    /// </summary>
    [HttpPost("2fa/disable")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new Educate.Application.Account.Commands.Disable2FA.Disable2FACommand(userId, request.Password);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // Login History
    
    /// <summary>
    /// Retrieves login history with pagination
    /// </summary>
    [HttpGet("login-history")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLoginHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new Educate.Application.Account.Queries.GetLoginHistory.GetLoginHistoryQuery(userId, page, pageSize);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    // User Settings
    
    /// <summary>
    /// Retrieves user application settings
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSettings()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new Educate.Application.Account.Queries.GetUserSettings.GetUserSettingsQuery(userId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Updates user application settings
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserSettings([FromBody] UpdateUserSettingsRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new Educate.Application.Account.Commands.UpdateUserSettings.UpdateUserSettingsCommand(
            userId,
            request.MarketingEmails,
            request.SystemNotifications,
            request.ExamReminders,
            request.PublicProfile,
            request.ShowExamResults,
            request.Language,
            request.Timezone);

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }
}

// Request DTOs
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateProfileRequest(string FirstName, string LastName, string? Username);
public record RequestEmailChangeRequest(string NewEmail, string Password);
public record ConfirmEmailChangeRequest(string OldEmailCode, string NewEmailCode);
public record DeleteAccountRequest(string Password, string? Reason);
public record Verify2FARequest(string Code);
public record Disable2FARequest(string Password);
public record UpdateUserSettingsRequest(
    bool MarketingEmails,
    bool SystemNotifications,
    bool ExamReminders,
    bool PublicProfile,
    bool ShowExamResults,
    string Language,
    string Timezone
);
