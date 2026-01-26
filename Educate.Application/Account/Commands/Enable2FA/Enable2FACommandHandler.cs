using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using System.Security.Cryptography;

namespace Educate.Application.Account.Commands.Enable2FA;

public class Enable2FACommandHandler : IRequestHandler<Enable2FACommand, Enable2FAResult>
{
    private readonly UserManager<User> _userManager;
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<Enable2FACommandHandler> _logger;

    public Enable2FACommandHandler(
        UserManager<User> userManager,
        IAppDbContext context,
        IEmailService emailService,
        IEncryptionService encryptionService,
        ILogger<Enable2FACommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<Enable2FAResult> Handle(Enable2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new Enable2FAResult(false, "User not found", null, null, null);

        if (user.IsTwoFactorEnabled)
            return new Enable2FAResult(false, "Two-factor authentication is already enabled", null, null, null);

        // Generate secret key for TOTP
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);

        // Generate QR code URI for authenticator apps
        var email = user.Email ?? user.UserName ?? "user";
        var qrCodeUri = $"otpauth://totp/Educate:{Uri.EscapeDataString(email)}?secret={base32Secret}&issuer=Educate";

        // Generate 10 recovery codes
        var recoveryCodes = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var code = GenerateRecoveryCode();
            recoveryCodes.Add(code);

            var codeHash = HashRecoveryCode(code);
            var backupCode = TwoFactorBackupCode.Create(request.UserId, codeHash);
            _context.TwoFactorBackupCodes.Add(backupCode);
        }

        // Enable 2FA
        var encryptedSecret = _encryptionService.Encrypt(base32Secret);
        user.Enable2FA(encryptedSecret);
        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Send email notification
        try
        {
            await _emailService.Send2FAEnabledNotificationAsync(user.Email!, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA enabled notification");
        }

        _logger.LogInformation("2FA enabled for user {UserId}", request.UserId);

        return new Enable2FAResult(
            true,
            "Two-factor authentication enabled successfully. Please scan the QR code with your authenticator app and save your recovery codes.",
            qrCodeUri,
            base32Secret,
            recoveryCodes
        );
    }

    private static string GenerateRecoveryCode()
    {
        // Generate 8-character alphanumeric code
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed similar-looking characters
        var code = new char[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            var buffer = new byte[8];
            rng.GetBytes(buffer);
            for (int i = 0; i < 8; i++)
            {
                code[i] = chars[buffer[i] % chars.Length];
            }
        }
        return new string(code);
    }

    private static string HashRecoveryCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
