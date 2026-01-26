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
        var subscriptions = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId)
            .Include(s => s.Level)
            .ThenInclude(l => l.Course)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        return subscriptions.Select(s =>
        {
            var daysRemaining = (s.EndDate - now).Days;
            var status = s.IsCurrentlyValid() 
                ? (daysRemaining <= 7 ? "Expiring Soon" : "Active")
                : "Expired";

            return new SubscriptionDto(
                s.Id,
                s.Level.Course.Name,
                s.Level.Name,
                s.StartDate,
                s.EndDate,
                s.AmountPaid,
                s.IsActive,
                daysRemaining,
                status,
                s.CanRenew()
            );
        }).ToList();
    }
}
