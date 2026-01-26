using MediatR;

namespace Educate.Application.Payments.Commands.ProcessWebhook;

public record ProcessWebhookCommand(string Body, string Signature) : IRequest<bool>;
