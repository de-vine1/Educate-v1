using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<AuthController> logger
    )
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
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

        return Ok(
            new LoginResponseDto
            {
                Success = true,
                Message =
                    user.CreatedAt == user.LastLoginAt
                        ? "Account created and logged in successfully"
                        : "Login successful",
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
}
