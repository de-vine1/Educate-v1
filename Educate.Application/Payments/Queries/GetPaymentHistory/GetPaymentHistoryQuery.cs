using MediatR;

namespace Educate.Application.Payments.Queries.GetPaymentHistory;

public record GetPaymentHistoryQuery(string UserId) : IRequest<List<PaymentDto>>;

public record PaymentDto(int Id, string CourseName, decimal Amount, string Status, DateTime Date);
