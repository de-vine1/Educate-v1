using MediatR;

namespace Educate.Application.Payments.Queries.GetPaymentHistory;

public record GetPaymentHistoryQuery(string UserId) : IRequest<List<PaymentDto>>;

public record PaymentDto(Guid Id, string CourseName, string LevelName, decimal Amount, string Status, DateTime Date);
