using MediatR;

namespace Educate.Application.Payments.Commands.ProcessPayment;

public record ProcessPaymentCommand(string UserId, int CourseId, string CardToken) : IRequest<string>;
