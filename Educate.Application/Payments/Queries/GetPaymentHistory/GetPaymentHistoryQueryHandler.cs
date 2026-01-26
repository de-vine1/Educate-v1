using Educate.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Payments.Queries.GetPaymentHistory;

public class GetPaymentHistoryQueryHandler : IRequestHandler<GetPaymentHistoryQuery, List<PaymentDto>>
{
    private readonly IAppDbContext _context;

    public GetPaymentHistoryQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PaymentDto>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .Include(p => p.Course)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id,
                p.Course.Name,
                p.Amount,
                p.Status,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
