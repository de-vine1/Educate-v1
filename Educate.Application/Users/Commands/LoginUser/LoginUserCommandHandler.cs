using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Users.Commands.LoginUser;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResponse>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        UserManager<User> userManager,
        IJwtService jwtService,
        IEmailService emailService,
        ILogger<LoginUserCommandHandler> logger)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // Find user by email or username
        var user = await _userManager.FindByEmailAsync(request.EmailOrUsername)
                   ?? await _userManager.FindByNameAsync(request.EmailOrUsername);

        if (user == null)
            return new LoginResponse(false, "Invalid credentials");

        // Check if account is locked
        if (await _userManager.IsLockedOutAsync(user))
            return new LoginResponse(false, "Account is locked due to multiple failed login attempts");

        // Validate password
        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return new LoginResponse(false, "Invalid credentials");
        }

        // Reset failed login attempts
        await _userManager.ResetAccessFailedCountAsync(user);

        // Update login time
        user.UpdateLastLogin();
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var token = await _jwtService.GenerateTokenAsync(user);
        var refreshToken = await _jwtService.GenerateRefreshTokenAsync(user);

        // Send login notification (Synchronous to ensure reliable delivery)
        try
        {
            await _emailService.SendLoginNotificationAsync(user.Email!, user.FirstName, request.IpAddress, request.UserAgent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send background login notification to {Email}", user.Email);
        }

        return new LoginResponse(
            true,
            "Login successful",
            token,
            refreshToken,
            DateTime.UtcNow.AddHours(24)
        );
    }
}
