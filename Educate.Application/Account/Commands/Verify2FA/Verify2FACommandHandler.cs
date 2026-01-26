using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OtpNet;

namespace Educate.Application.Account.Commands.Verify2FA;

public class Verify2FACommandHandler : IRequestHandler<Verify2FACommand, Verify2FAResult>
{
    private readonly UserManager<User> _userManager;
    private readonly IEncryptionService _encryptionService;

    public Verify2FACommandHandler(UserManager<User> userManager, IEncryptionService encryptionService)
    {
        _userManager = userManager;
        _encryptionService = encryptionService;
    }

    public async Task<Verify2FAResult> Handle(Verify2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new Verify2FAResult(false, "User not found");

        if (!user.IsTwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return new Verify2FAResult(false, "Two-factor authentication is not enabled");

        // Decrypt TOTP secret key
        var decryptedSecret = _encryptionService.Decrypt(user.TwoFactorSecretKey);

        // Verify TOTP code
        var secretKeyBytes = Base32Encoding.ToBytes(decryptedSecret);
        var totp = new Totp(secretKeyBytes);
        var isValid = totp.VerifyTotp(request.Code, out _, new VerificationWindow(2, 2));

        if (!isValid)
            return new Verify2FAResult(false, "Invalid verification code");

        return new Verify2FAResult(true, "Verification successful");
    }
}
