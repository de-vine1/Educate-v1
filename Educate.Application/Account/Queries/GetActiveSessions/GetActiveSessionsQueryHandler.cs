using Educate.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Account.Queries.GetActiveSessions;

public class GetActiveSessionsQueryHandler : IRequestHandler<GetActiveSessionsQuery, List<SessionDto>>
{
    private readonly IAppDbContext _context;

    public GetActiveSessionsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SessionDto>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _context.RefreshTokens
            .Where(rt => rt.UserId == request.UserId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.LastActivity)
            .Select(rt => new SessionDto(
                rt.Id,
                rt.DeviceInfo ?? "Unknown Device",
                rt.IpAddress ?? "Unknown",
                rt.LastActivity,
                rt.CreatedAt,
                false // Will be set by controller based on current token
            ))
            .ToListAsync(cancellationToken);

        return sessions;
    }
}
