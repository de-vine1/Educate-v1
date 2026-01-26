using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Payments.Commands.VerifyPayment;

public class VerifyPaymentCommandHandler : IRequestHandler<VerifyPaymentCommand, PaymentVerificationResponse>
{
    private readonly IPaymentService _paymentService;
    private readonly IAppDbContext _context;

    public VerifyPaymentCommandHandler(IPaymentService paymentService, IAppDbContext context)
    {
        _paymentService = paymentService;
        _context = context;
    }

    public async Task<PaymentVerificationResponse> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        var verificationResult = await _paymentService.VerifyPaymentAsync(request.Reference);

        if (verificationResult.Status) // Payment verified successfully
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == request.Reference, cancellationToken);

            if (payment == null)
            {
                // This shouldn't happen if VerifyPaymentAsync succeeded and used the same DB context, 
                // but safeguard just in case.
                return verificationResult;
            }

            // Check if subscription already created for this payment (idempotency)
            var existingSub = await _context.Subscriptions
                .AnyAsync(s => s.UserId == payment.UserId && s.LevelId == payment.LevelId && s.IsActive, cancellationToken);
            
            // Or simpler: check if user recently subscribed. 
            // Better: Check if we already processed this payment for subscription.
            // Since we don't link Subscription <-> Payment directly in DB yet (explicitly), 
            // we rely on checking active subscriptions.
            
            if (!existingSub)
            {
                var startDate = DateTime.UtcNow;
                var endDate = startDate.AddMonths(6); // 6 Month validity

                var subscription = Subscription.Create(
                    payment.UserId,
                    payment.LevelId,
                    payment.Amount,
                    startDate,
                    endDate
                );

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return verificationResult;
    }
}
