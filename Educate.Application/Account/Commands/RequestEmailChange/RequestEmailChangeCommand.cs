using MediatR;

namespace Educate.Application.Account.Commands.RequestEmailChange;

public record RequestEmailChangeCommand(
    string UserId,
    string NewEmail,
    string Password
) : IRequest<RequestEmailChangeResult>;

public record RequestEmailChangeResult(
    bool Success,
    string Message
);
