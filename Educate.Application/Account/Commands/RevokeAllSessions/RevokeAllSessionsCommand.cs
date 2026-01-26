using MediatR;

namespace Educate.Application.Account.Commands.RevokeAllSessions;

public record RevokeAllSessionsCommand(
    string UserId,
    Guid? CurrentSessionId = null
) : IRequest<RevokeAllSessionsResult>;

public record RevokeAllSessionsResult(
    bool Success,
    string Message,
    int SessionsRevoked
);
