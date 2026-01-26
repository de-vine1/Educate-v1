using MediatR;

namespace Educate.Application.Account.Commands.ConfirmEmailChange;

public record ConfirmEmailChangeCommand(
    string UserId,
    string OldEmailCode,
    string NewEmailCode
) : IRequest<ConfirmEmailChangeResult>;

public record ConfirmEmailChangeResult(
    bool Success,
    string Message
);
