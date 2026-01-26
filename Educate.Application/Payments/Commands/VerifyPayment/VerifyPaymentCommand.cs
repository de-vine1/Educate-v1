using Educate.Application.Models.DTOs;
using MediatR;

namespace Educate.Application.Payments.Commands.VerifyPayment;

public record VerifyPaymentCommand(string Reference) : IRequest<PaymentVerificationResponse>;
