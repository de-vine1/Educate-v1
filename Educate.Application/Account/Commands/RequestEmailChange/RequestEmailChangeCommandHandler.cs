using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Account.Commands.RequestEmailChange;

public class RequestEmailChangeCommandHandler : IRequestHandler<RequestEmailChangeCommand, RequestEmailChangeResult>
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestEmailChangeCommandHandler> _logger;
    private static readonly Dictionary<string, (string OldEmailCode, string NewEmailCode, string NewEmail, DateTime Expiry)> _pendingChanges = new();

    public RequestEmailChangeCommandHandler(
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<RequestEmailChangeCommandHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RequestEmailChangeResult> Handle(RequestEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new RequestEmailChangeResult(false, "User not found");

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return new RequestEmailChangeResult(false, "Incorrect password");

        // Validate new email
        if (string.IsNullOrWhiteSpace(request.NewEmail) || !request.NewEmail.Contains("@"))
            return new RequestEmailChangeResult(false, "Invalid email address");

        if (request.NewEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            return new RequestEmailChangeResult(false, "New email must be different from current email");

        // Check if new email is already in use
        var existingUser = await _userManager.FindByEmailAsync(request.NewEmail);
        if (existingUser != null)
            return new RequestEmailChangeResult(false, "Email address is already in use");

        // Generate verification codes
        var oldEmailCode = GenerateCode();
        var newEmailCode = GenerateCode();

        // Store pending change (expires in 15 minutes)
        _pendingChanges[request.UserId] = (oldEmailCode, newEmailCode, request.NewEmail, DateTime.UtcNow.AddMinutes(15));

        // Send verification codes to both emails
        try
        {
            await _emailService.SendEmailVerificationCodeAsync(user.Email!, user.FirstName, oldEmailCode, "current");
            await _emailService.SendEmailVerificationCodeAsync(request.NewEmail, user.FirstName, newEmailCode, "new");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification codes");
            return new RequestEmailChangeResult(false, "Failed to send verification codes. Please try again.");
        }

        _logger.LogInformation("Email change requested for user {UserId}", request.UserId);

        return new RequestEmailChangeResult(true, "Verification codes sent to both email addresses. Please check your inbox.");
    }

    private static string GenerateCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }

    public static Dictionary<string, (string OldEmailCode, string NewEmailCode, string NewEmail, DateTime Expiry)> GetPendingChanges()
    {
        return _pendingChanges;
    }
}
