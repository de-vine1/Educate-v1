using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Account.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private readonly UserManager<User> _userManager;
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        UserManager<User> userManager,
        IAppDbContext context,
        IEmailService emailService,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new ChangePasswordResult(false, "User not found");

        // Verify current password
        var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!isCurrentPasswordValid)
            return new ChangePasswordResult(false, "Current password is incorrect");

        // Ensure new password is different
        if (request.CurrentPassword == request.NewPassword)
            return new ChangePasswordResult(false, "New password must be different from current password");

        // Change password
        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ChangePasswordResult(false, $"Password change failed: {errors}");
        }

        // Invalidate all refresh tokens except current session (handled by controller)
        var tokensToRemove = await _context.RefreshTokens
            .Where(rt => rt.UserId == request.UserId)
            .ToListAsync(cancellationToken);
        
        _context.RefreshTokens.RemoveRange(tokensToRemove);
        await _context.SaveChangesAsync(cancellationToken);

        // Send email notification
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(user.Email!, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password change notification to {Email}", user.Email);
        }

        _logger.LogInformation("Password changed for user {UserId}", request.UserId);

        return new ChangePasswordResult(true, "Password changed successfully. All sessions have been logged out for security.");
    }
}
