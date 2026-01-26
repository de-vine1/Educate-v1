using Educate.Application.Models.DTOs;
using MediatR;

namespace Educate.Application.Payments.Commands.InitializePayment;

public record InitializePaymentCommand(string UserId, Guid LevelId) : IRequest<PaymentInitializationResponse>;
