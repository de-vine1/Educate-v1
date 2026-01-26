using MediatR;

namespace Educate.Application.Users.Queries.GetUserProfile;

public record GetUserProfileQuery(string UserId) : IRequest<UserProfileDto>;

public record UserProfileDto(string Id, string FirstName, string LastName, string Username, string Email, bool EmailConfirmed);
