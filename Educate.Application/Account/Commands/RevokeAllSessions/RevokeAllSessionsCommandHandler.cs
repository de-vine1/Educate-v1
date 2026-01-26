using Educate.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Account.Commands.RevokeAllSessions;

public class RevokeAllSessionsCommandHandler : IRequestHandler<RevokeAllSessionsCommand, RevokeAllSessionsResult>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<RevokeAllSessionsCommandHandler> _logger;

    public RevokeAllSessionsCommandHandler(IAppDbContext context, ILogger<RevokeAllSessionsCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RevokeAllSessionsResult> Handle(RevokeAllSessionsCommand request, CancellationToken cancellationToken)
    {
        var query = _context.RefreshTokens.Where(rt => rt.UserId == request.UserId);

        // Keep current session if specified
        if (request.CurrentSessionId.HasValue)
        {
            query = query.Where(rt => rt.Id != request.CurrentSessionId.Value);
        }

        var sessions = await query.ToListAsync(cancellationToken);
        var count = sessions.Count;

        _context.RefreshTokens.RemoveRange(sessions);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked {Count} sessions for user {UserId}", count, request.UserId);

        return new RevokeAllSessionsResult(true, $"All sessions revoked successfully ({count} sessions)", count);
    }
}
