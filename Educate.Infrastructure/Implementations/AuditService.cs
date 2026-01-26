using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;

namespace Educate.Infrastructure.Implementations;

public class AuditService : IAuditService
{
    private readonly IAppDbContext _context;

    public AuditService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string userId,
        string action,
        string details,
        string ipAddress,
        string userAgent
    )
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(CancellationToken.None);
    }
}
