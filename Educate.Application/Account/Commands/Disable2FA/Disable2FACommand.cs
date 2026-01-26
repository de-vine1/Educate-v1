using MediatR;

namespace Educate.Application.Account.Commands.Disable2FA;

public record Disable2FACommand(
    string UserId,
    string Password
) : IRequest<Disable2FAResult>;

public record Disable2FAResult(
    bool Success,
    string Message
);
