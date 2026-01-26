using Educate.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Subscriptions.Queries.GetUserSubscriptions;

public class GetUserSubscriptionsQueryHandler : IRequestHandler<GetUserSubscriptionsQuery, List<SubscriptionDto>>
{
    private readonly IAppDbContext _context;

    public GetUserSubscriptionsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SubscriptionDto>> Handle(GetUserSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId)
            .Include(s => s.Course)
            .Select(s => new SubscriptionDto(
                s.Id,
                s.Course.Name,
                s.StartDate,
                s.EndDate,
                s.AmountPaid,
                s.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}
