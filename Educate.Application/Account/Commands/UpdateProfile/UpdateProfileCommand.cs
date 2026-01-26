using MediatR;

namespace Educate.Application.Account.Commands.UpdateProfile;

public record UpdateProfileCommand(
    string UserId,
    string FirstName,
    string LastName,
    string? Username
) : IRequest<UpdateProfileResult>;

public record UpdateProfileResult(
    bool Success,
    string Message
);
