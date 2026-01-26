using MediatR;

namespace Educate.Application.Account.Commands.Verify2FA;

public record Verify2FACommand(
    string UserId,
    string Code
) : IRequest<Verify2FAResult>;

public record Verify2FAResult(
    bool Success,
    string Message
);
