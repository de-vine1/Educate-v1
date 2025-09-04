using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Educate.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly AppDbContext _context;
    private readonly RateLimitingService _rateLimitingService;

    public AuthController(
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<AuthController> logger,
        AppDbContext context,
        RateLimitingService rateLimitingService
    )
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
        _context = context;
        _rateLimitingService = rateLimitingService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponseDto>> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if username already exists
        var existingUserByUsername = await _userManager.FindByNameAsync(model.Username);
        if (existingUserByUsername != null)
        {
            return BadRequest(
                new RegisterResponseDto { Success = false, Message = "Username already exists" }
            );
        }

        // Check if email already exists
        var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
        if (existingUserByEmail != null)
        {
            return BadRequest(
                new RegisterResponseDto { Success = false, Message = "Email already exists" }
            );
        }

        // Create new user
        var user = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            UserName = model.Username,
            Email = model.Email,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(
                new RegisterResponseDto
                {
                    Success = false,
                    Message = $"Registration failed: {errors}",
                }
            );
        }

        // Generate email confirmation token
        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Send confirmation email
        try
        {
            await _emailService.SendEmailConfirmationAsync(
                user.Email!,
                user.FirstName,
                confirmationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
            // Continue with registration even if email fails
        }

        return Ok(
            new RegisterResponseDto
            {
                Success = true,
                Message =
                    "Registration successful. Your email is not yet verified. Some features may be restricted until verification is complete.",
                UserId = user.Id,
                EmailConfirmationRequired = true,
            }
        );
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Invalid confirmation token");
        }

        var users = _userManager.Users.Where(u => !u.EmailConfirmed).ToList();

        foreach (var user in users)
        {
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // Log confirmation time
                user.EmailConfirmedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Send welcome email
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

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find user by email or username
        var user =
            await _userManager.FindByEmailAsync(model.EmailOrUsername)
            ?? await _userManager.FindByNameAsync(model.EmailOrUsername);

        if (user == null)
        {
            return BadRequest(
                new LoginResponseDto { Success = false, Message = "Invalid credentials" }
            );
        }

        // Check if account is locked
        if (await _userManager.IsLockedOutAsync(user))
        {
            return BadRequest(
                new LoginResponseDto
                {
                    Success = false,
                    Message = "Account is locked due to multiple failed login attempts",
                }
            );
        }

        // Validate password
        var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return BadRequest(
                new LoginResponseDto { Success = false, Message = "Invalid credentials" }
            );
        }

        // Reset failed login attempts on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // Update login time and metadata
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate JWT token and refresh token
        var jwtService = HttpContext.RequestServices.GetRequiredService<IJwtService>();
        var token = await jwtService.GenerateTokenAsync(user);
        var refreshToken = await jwtService.GenerateRefreshTokenAsync(user);

        // Send login notification
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        try
        {
            await _emailService.SendLoginNotificationAsync(
                user.Email!,
                user.FirstName,
                ipAddress,
                userAgent
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send login notification to {Email}", user.Email);
        }

        return Ok(
            new LoginResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
            }
        );
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action("GoogleCallback", "Auth");
        var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = redirectUrl,
        };
        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
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
            user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                EmailConfirmedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                OAuthProvider = "Google",
                OAuthId = googleId,
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
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
            user.OAuthProvider = "Google";
            user.OAuthId = googleId;
            await _userManager.UpdateAsync(user);
        }

        user.LastLoginAt = DateTime.UtcNow;
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

    [HttpPost("refresh-token")]
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

    [HttpPost("forgot-password")]
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

    [HttpPost("reset-password")]
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

    [HttpPost("set-password")]
    [Authorize]
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

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<LogoutResponseDto>> Logout([FromBody] LogoutDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(
                new LogoutResponseDto { Success = false, Message = "User not authenticated" }
            );
        }

        // Revoke the specific refresh token
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.UserId == userId && rt.Token == model.RefreshToken
        );

        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            await _context.SaveChangesAsync();
        }

        // Log security event
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        _logger.LogInformation(
            "User {UserId} logged out from IP {IpAddress} at {Timestamp}",
            userId,
            ipAddress,
            DateTime.UtcNow
        );

        return Ok(new LogoutResponseDto { Success = true, Message = "Logged out successfully" });
    }
}
