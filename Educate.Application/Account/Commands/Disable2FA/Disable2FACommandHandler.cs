using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Account.Commands.Disable2FA;

public class Disable2FACommandHandler : IRequestHandler<Disable2FACommand, Disable2FAResult>
{
    private readonly UserManager<User> _userManager;
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<Disable2FACommandHandler> _logger;

    public Disable2FACommandHandler(
        UserManager<User> userManager,
        IAppDbContext context,
        IEmailService emailService,
        ILogger<Disable2FACommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Disable2FAResult> Handle(Disable2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new Disable2FAResult(false, "User not found");

        if (!user.IsTwoFactorEnabled)
            return new Disable2FAResult(false, "Two-factor authentication is not enabled");

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return new Disable2FAResult(false, "Incorrect password");

        // Disable 2FA
        user.Disable2FA();
        await _userManager.UpdateAsync(user);

        // Remove all backup codes
        var backupCodes = await _context.TwoFactorBackupCodes
            .Where(bc => bc.UserId == request.UserId)
            .ToListAsync(cancellationToken);
        _context.TwoFactorBackupCodes.RemoveRange(backupCodes);

        await _context.SaveChangesAsync(cancellationToken);

        // Send email notification
        try
        {
            await _emailService.Send2FADisabledNotificationAsync(user.Email!, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA disabled notification");
        }

        _logger.LogWarning("2FA disabled for user {UserId}", request.UserId);

        return new Disable2FAResult(true, "Two-factor authentication disabled successfully");
    }
}
