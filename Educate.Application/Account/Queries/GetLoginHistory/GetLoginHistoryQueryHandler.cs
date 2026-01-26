using Educate.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Account.Queries.GetLoginHistory;

public class GetLoginHistoryQueryHandler : IRequestHandler<GetLoginHistoryQuery, GetLoginHistoryResult>
{
    private readonly IAppDbContext _context;

    public GetLoginHistoryQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<GetLoginHistoryResult> Handle(GetLoginHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LoginAttempts
            .Where(la => la.UserId == request.UserId)
            .OrderByDescending(la => la.Timestamp);

        var totalRecords = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(la => new LoginHistoryDto(
                la.Timestamp,
                la.IpAddress,
                la.UserAgent,
                la.Location,
                la.IsSuccessful,
                la.FailureReason
            ))
            .ToListAsync(cancellationToken);

        return new GetLoginHistoryResult(
            request.Page,
            totalPages,
            totalRecords,
            items
        );
    }
}
