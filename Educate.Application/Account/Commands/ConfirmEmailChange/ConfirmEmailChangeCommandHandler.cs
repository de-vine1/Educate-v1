using Educate.Application.Account.Commands.RequestEmailChange;
using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Account.Commands.ConfirmEmailChange;

public class ConfirmEmailChangeCommandHandler : IRequestHandler<ConfirmEmailChangeCommand, ConfirmEmailChangeResult>
{
    private readonly UserManager<User> _userManager;
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ConfirmEmailChangeCommandHandler> _logger;

    public ConfirmEmailChangeCommandHandler(
        UserManager<User> userManager,
        IAppDbContext context,
        IEmailService emailService,
        ILogger<ConfirmEmailChangeCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ConfirmEmailChangeResult> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new ConfirmEmailChangeResult(false, "User not found");

        // Get pending changes
        var pendingChanges = RequestEmailChangeCommandHandler.GetPendingChanges();
        
        if (!pendingChanges.TryGetValue(request.UserId, out var changeData))
            return new ConfirmEmailChangeResult(false, "No pending email change found. Please request a new change.");

        // Check expiry
        if (DateTime.UtcNow > changeData.Expiry)
        {
            pendingChanges.Remove(request.UserId);
            return new ConfirmEmailChangeResult(false, "Verification codes have expired. Please request a new change.");
        }

        // Verify both codes
        if (request.OldEmailCode != changeData.OldEmailCode || request.NewEmailCode != changeData.NewEmailCode)
            return new ConfirmEmailChangeResult(false, "Invalid verification codes");

        var oldEmail = user.Email;

        // Update email
        var setEmailResult = await _userManager.SetEmailAsync(user, changeData.NewEmail);
        if (!setEmailResult.Succeeded)
        {
            var errors = string.Join(", ", setEmailResult.Errors.Select(e => e.Description));
            return new ConfirmEmailChangeResult(false, $"Failed to update email: {errors}");
        }

        // Update username to new email if username was email
        if (user.UserName == oldEmail)
        {
            await _userManager.SetUserNameAsync(user, changeData.NewEmail);
        }

        // Mark email as confirmed
        user.ConfirmEmail();
        await _userManager.UpdateAsync(user);

        // Invalidate all sessions for security
        var tokensToRemove = await _context.RefreshTokens
            .Where(rt => rt.UserId == request.UserId)
            .ToListAsync(cancellationToken);
        
        _context.RefreshTokens.RemoveRange(tokensToRemove);
        await _context.SaveChangesAsync(cancellationToken);

        // Remove pending change
        pendingChanges.Remove(request.UserId);

        // Send confirmation emails
        try
        {
            await _emailService.SendEmailChangedNotificationAsync(oldEmail!, user.FirstName, changeData.NewEmail);
            await _emailService.SendEmailChangedNotificationAsync(changeData.NewEmail, user.FirstName, changeData.NewEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email change notifications");
        }

        _logger.LogInformation("Email changed for user {UserId} from {OldEmail} to {NewEmail}", request.UserId, oldEmail, changeData.NewEmail);

        return new ConfirmEmailChangeResult(true, "Email changed successfully. All sessions have been logged out for security.");
    }
}
