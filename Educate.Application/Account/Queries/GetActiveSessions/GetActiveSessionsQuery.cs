using MediatR;

namespace Educate.Application.Account.Queries.GetActiveSessions;

public record GetActiveSessionsQuery(string UserId) : IRequest<List<SessionDto>>;

public record SessionDto(
    Guid SessionId,
    string DeviceInfo,
    string IpAddress,
    DateTime LastActivity,
    DateTime CreatedAt,
    bool IsCurrent
);
