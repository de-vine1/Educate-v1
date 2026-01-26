using Educate.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Account.Commands.RevokeSession;

public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, RevokeSessionResult>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<RevokeSessionCommandHandler> _logger;

    public RevokeSessionCommandHandler(IAppDbContext context, ILogger<RevokeSessionCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RevokeSessionResult> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == request.SessionId && rt.UserId == request.UserId, cancellationToken);

        if (session == null)
            return new RevokeSessionResult(false, "Session not found");

        _context.RefreshTokens.Remove(session);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Session revoked for user {UserId}", request.UserId);

        return new RevokeSessionResult(true, "Session revoked successfully");
    }
}
