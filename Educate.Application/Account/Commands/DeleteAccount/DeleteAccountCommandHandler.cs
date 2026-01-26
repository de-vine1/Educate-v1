using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Account.Commands.DeleteAccount;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, DeleteAccountResult>
{
    private readonly UserManager<User> _userManager;
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        UserManager<User> userManager,
        IAppDbContext context,
        IEmailService emailService,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DeleteAccountResult> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new DeleteAccountResult(false, "User not found");

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return new DeleteAccountResult(false, "Incorrect password");

        var oldEmail = user.Email;
        var oldName = user.FirstName;

        // Soft delete user (anonymize data)
        user.SoftDelete();
        await _userManager.UpdateAsync(user);

        // Invalidate all sessions
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == request.UserId)
            .ToListAsync(cancellationToken);
        _context.RefreshTokens.RemoveRange(tokens);

        // Cancel active subscriptions
        var activeSubscriptions = await _context.Subscriptions
            .Where(s => s.UserId == request.UserId && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var subscription in activeSubscriptions)
        {
            subscription.Deactivate();
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Send deletion confirmation email
        try
        {
            await _emailService.SendAccountDeletionConfirmationAsync(oldEmail!, oldName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account deletion confirmation to {Email}", oldEmail);
        }

        _logger.LogWarning("Account deleted for user {UserId}. Reason: {Reason}", request.UserId, request.Reason ?? "Not provided");

        return new DeleteAccountResult(true, "Account deleted successfully. Your data has been anonymized for compliance.");
    }
}
