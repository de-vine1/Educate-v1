using MediatR;

namespace Educate.Application.Account.Commands.RevokeSession;

public record RevokeSessionCommand(
    string UserId,
    Guid SessionId
) : IRequest<RevokeSessionResult>;

public record RevokeSessionResult(
    bool Success,
    string Message
);
