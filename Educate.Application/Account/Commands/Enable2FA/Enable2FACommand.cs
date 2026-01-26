using MediatR;

namespace Educate.Application.Account.Commands.Enable2FA;

public record Enable2FACommand(string UserId) : IRequest<Enable2FAResult>;

public record Enable2FAResult(
    bool Success,
    string Message,
    string? QrCodeUri,
    string? ManualEntryKey,
    List<string>? RecoveryCodes
);
