using MediatR;

namespace Educate.Application.Account.Commands.DeleteAccount;

public record DeleteAccountCommand(
    string UserId,
    string Password,
    string? Reason
) : IRequest<DeleteAccountResult>;

public record DeleteAccountResult(
    bool Success,
    string Message
);
