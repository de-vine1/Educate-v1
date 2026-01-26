using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, string>
{
    private readonly IAppDbContext _context;
    private readonly IEncryptionService _encryptionService;

    public ProcessPaymentCommandHandler(IAppDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<string> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

        if (course == null)
            throw new ArgumentException("Course not found");

        var transactionId = Guid.NewGuid().ToString();
        var encryptedToken = _encryptionService.Encrypt(request.CardToken);

        var payment = Payment.Create(
            request.UserId,
            request.CourseId,
            course.AnnualPrice,
            "Card",
            transactionId,
            encryptedToken
        );

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return transactionId;
    }
}
