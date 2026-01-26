using MediatR;

namespace Educate.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand(string FirstName, string LastName, string Username, string Email, string Password) : IRequest<RegisterResponse>;

public record RegisterResponse(bool Success, string Message, string? UserId = null, bool EmailConfirmationRequired = false);
