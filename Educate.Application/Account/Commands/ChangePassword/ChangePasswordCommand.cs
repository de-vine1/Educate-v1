using MediatR;

namespace Educate.Application.Account.Commands.ChangePassword;

public record ChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<ChangePasswordResult>;

public record ChangePasswordResult(
    bool Success,
    string Message
);
