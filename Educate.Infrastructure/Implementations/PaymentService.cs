using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Educate.Infrastructure.Implementations;

public class PaymentService : IPaymentService
{
    private readonly IAppDbContext _context;
    private readonly IEncryptionService _encryptionService;

    public PaymentService(IAppDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<string> ProcessPaymentAsync(string userId, int courseId, string cardToken)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
            throw new ArgumentException("Course not found");

        var transactionId = Guid.NewGuid().ToString();
        var encryptedToken = _encryptionService.Encrypt(cardToken);

        var payment = Payment.Create(
            userId,
            courseId,
            course.AnnualPrice,
            "Card",
            transactionId,
            encryptedToken
        );

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(CancellationToken.None);

        return transactionId;
    }

    public async Task<bool> VerifyPaymentAsync(string transactionId)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p =>
            p.TransactionId == transactionId
        );

        return payment?.Status == "Success";
    }
}
