using MediatR;

namespace Educate.Application.Users.Commands.LoginUser;

public record LoginUserCommand(string EmailOrUsername, string Password, string IpAddress, string UserAgent) : IRequest<LoginResponse>;

public record LoginResponse(bool Success, string Message, string? Token = null, string? RefreshToken = null, DateTime? ExpiresAt = null);
