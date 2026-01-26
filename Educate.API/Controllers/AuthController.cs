using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Educate.Infrastructure.Services;
using Educate.Application.Users.Commands.LoginUser;
using Microsoft.EntityFrameworkCore;
using Educate.Application.Users.Commands.RegisterUser;
using Educate.Application.Users.Queries.GetUserProfile;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly AppDbContext _context;
    private readonly RateLimitingService _rateLimitingService;

    public AuthController(
        IMediator mediator,
        ILogger<AuthController> logger,
        UserManager<User> userManager,
        IEmailService emailService,
        AppDbContext context,
        RateLimitingService rateLimitingService
    )
    {
        _mediator = mediator;
        _logger = logger;
        _userManager = userManager;
        _emailService = emailService;
        _context = context;
        _rateLimitingService = rateLimitingService;
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="model">Registration details</param>
    /// <returns>Registration result</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new RegisterUserCommand(
            model.FirstName,
            model.LastName,
            model.Username,
            model.Email,
            model.Password
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Confirms user email address
    /// </summary>
    /// <param name="token">The confirmation token</param>
    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
            return BadRequest("Invalid confirmation token");

        var users = _userManager.Users.ToList().Where(u => !u.EmailConfirmed);

        foreach (var user in users)
        {
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                user.ConfirmEmail();
                await _userManager.UpdateAsync(user);

                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                }

                return Ok(new { message = "Email confirmed successfully" });
            }
        }

        return BadRequest("Invalid or expired confirmation token");
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="model">Login credentials</param>
    /// <returns>JWT token and refresh token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var command = new LoginUserCommand(model.EmailOrUsername, model.Password, ipAddress, userAgent);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves current user profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var profile = await _mediator.Send(new GetUserProfileQuery(userId));
            return Ok(profile);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Initiates Google OAuth login
    /// </summary>
    [HttpGet("google-login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action("GoogleCallback", "Auth");
        var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = redirectUrl,
        };
        return Challenge(properties, "Google");
    }

    /// <summary>
    /// Callback for Google OAuth
    /// </summary>
    [HttpGet("google-callback")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponseDto>> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("Google");
        if (!result.Succeeded)
        {
            return BadRequest(
                new LoginResponseDto { Success = false, Message = "Google authentication failed" }
            );
        }

        var claims = result.Principal!.Claims;
        var email = claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)
            ?.Value;
        var firstName =
            claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value
            ?? "";
        var lastName =
            claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Surname)?.Value
            ?? "";
        var googleId = claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(
                new LoginResponseDto { Success = false, Message = "Email not provided by Google" }
            );
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            var username = email.Split('@')[0] + Guid.NewGuid().ToString()[..4];
            user = Educate.Domain.Entities.User.Create(firstName, lastName, email, username);
            
            if (string.IsNullOrEmpty(googleId))
                 return BadRequest(new LoginResponseDto { Success = false, Message = "Google ID missing" });

            user.LinkOAuth("Google", googleId);

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                // Log confirmation time
                user.ConfirmEmail();
                await _userManager.UpdateAsync(user);

                // Send welcome email
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                }
            }
            else
            {
                return BadRequest(
                    new LoginResponseDto
                    {
                        Success = false,
                        Message = "Failed to create user account",
                    }
                );
            }
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
            }
        }
        else if (string.IsNullOrEmpty(user.OAuthProvider))
        {
            // Link existing account to Google OAuth
            user.LinkOAuth("Google", googleId);
            await _userManager.UpdateAsync(user);
        }

        user.UpdateLastLogin();
        await _userManager.UpdateAsync(user);

        var jwtService = HttpContext.RequestServices.GetRequiredService<IJwtService>();
        var token = await jwtService.GenerateTokenAsync(user);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        try
        {
            await _emailService.SendLoginNotificationAsync(
                user.Email!,
                user.FirstName,
                ipAddress,
                "Google OAuth"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send login notification to {Email}", user.Email);
        }

        var refreshTokenGoogle = await jwtService.GenerateRefreshTokenAsync(user);

        var hasPassword = await _userManager.HasPasswordAsync(user);
        var message =
            user.CreatedAt == user.LastLoginAt
                ? "Account created and logged in successfully"
                : "Login successful";

        if (!hasPassword)
        {
            message += ". Please set a password to enable standard login.";
        }

        return Ok(
            new LoginResponseDto
            {
                Success = true,
                Message = message,
                Token = token,
                RefreshToken = refreshTokenGoogle,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
            }
        );
    }

    /// <summary>
    /// Refreshes an expired JWT token
    /// </summary>
    /// <param name="model">Refresh token details</param>
    /// <returns>New JWT token</returns>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var jwtService = HttpContext.RequestServices.GetRequiredService<IJwtService>();

        // Extract user ID from expired token (simplified)
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(model.Token);
        var userId = jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new LoginResponseDto { Success = false, Message = "Invalid token" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return BadRequest(new LoginResponseDto { Success = false, Message = "User not found" });
        }

        // Validate refresh token
        var isValidRefreshToken = await jwtService.ValidateRefreshTokenAsync(
            userId,
            model.RefreshToken
        );
        if (!isValidRefreshToken)
        {
            return BadRequest(
                new LoginResponseDto { Success = false, Message = "Invalid refresh token" }
            );
        }

        // Generate new tokens
        var newToken = await jwtService.GenerateTokenAsync(user);
        var newRefreshToken = await jwtService.GenerateRefreshTokenAsync(user);

        return Ok(
            new LoginResponseDto
            {
                Success = true,
                Message = "Token refreshed successfully",
                Token = newToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
            }
        );
    }

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    /// <param name="model">Email address</param>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword(
        [FromBody] ForgotPasswordDto model
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Rate limiting check
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimitKey = $"forgot_password_{clientIp}";

        if (_rateLimitingService.IsRateLimited(rateLimitKey))
        {
            return BadRequest(
                new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Too many password reset requests. Please try again later.",
                }
            );
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        // Always return the same message to prevent email enumeration
        const string genericMessage =
            "If this email exists, you will receive a password reset link.";

        if (user != null)
        {
            // Generate JWT password reset token
            var jwtService = HttpContext.RequestServices.GetRequiredService<IJwtService>();
            var resetToken = jwtService.GeneratePasswordResetToken(user);

            // Send password reset email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email!, resetToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }
        }

        return Ok(new ForgotPasswordResponseDto { Success = true, Message = genericMessage });
    }

    /// <summary>
    /// Resets user password using token
    /// </summary>
    /// <param name="model">Reset token and new password</param>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ResetPassword(
        [FromBody] ResetPasswordDto model
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var jwtService = HttpContext.RequestServices.GetRequiredService<IJwtService>();

        // Validate JWT reset token
        if (!jwtService.ValidatePasswordResetToken(model.Token, out string userId))
        {
            return BadRequest(
                new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired reset token",
                }
            );
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return BadRequest(
                new ForgotPasswordResponseDto { Success = false, Message = "User not found" }
            );
        }

        // Reset password
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(
                new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = $"Password reset failed: {errors}",
                }
            );
        }

        // Invalidate all refresh tokens for security
        var existingTokens = await _context
            .RefreshTokens.Where(rt => rt.UserId == userId)
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(existingTokens);
        await _context.SaveChangesAsync();

        // Get request metadata
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        // Log security event
        _logger.LogWarning(
            "Password reset completed for user {UserId} from IP {IpAddress} using {UserAgent} at {Timestamp}",
            userId,
            ipAddress,
            userAgent,
            DateTime.UtcNow
        );

        // Send confirmation email
        try
        {
            await _emailService.SendPasswordResetConfirmationAsync(
                user.Email!,
                user.FirstName,
                ipAddress,
                userAgent
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send password reset confirmation email to {Email}",
                user.Email
            );
        }

        return Ok(
            new ForgotPasswordResponseDto
            {
                Success = true,
                Message =
                    "Password has been reset successfully. All active sessions have been invalidated for security.",
            }
        );
    }

    /// <summary>
    /// Sets a password for an OAuth user
    /// </summary>
    /// <param name="model">New password</param>
    [HttpPost("set-password")]
    [Authorize]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ForgotPasswordResponseDto>> SetPassword(
        [FromBody] SetPasswordDto model
    )
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(
                new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "User not authenticated",
                }
            );
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return BadRequest(
                new ForgotPasswordResponseDto { Success = false, Message = "User not found" }
            );
        }

        // Check if user already has a password
        var hasPassword = await _userManager.HasPasswordAsync(user);
        if (hasPassword)
        {
            return BadRequest(
                new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Password already set. Use change password instead.",
                }
            );
        }

        // Add password to OAuth user
        var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(
                new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = $"Failed to set password: {errors}",
                }
            );
        }

        // Log security event
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        _logger.LogInformation(
            "Password set for OAuth user {UserId} from IP {IpAddress} at {Timestamp}",
            userId,
            ipAddress,
            DateTime.UtcNow
        );

        // Send account setup confirmation email
        try
        {
            await _emailService.SendPasswordSetConfirmationAsync(user.Email!, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send password set confirmation email to {Email}",
                user.Email
            );
        }

        return Ok(
            new ForgotPasswordResponseDto
            {
                Success = true,
                Message = "Password set successfully. You can now login with email and password.",
            }
        );
    }
}
