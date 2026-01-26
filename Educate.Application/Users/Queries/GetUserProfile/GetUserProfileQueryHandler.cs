using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Educate.Application.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly UserManager<User> _userManager;

    public GetUserProfileQueryHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            throw new ArgumentException("User not found");

        return new UserProfileDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.UserName!,
            user.Email!,
            user.EmailConfirmed
        );
    }
}
