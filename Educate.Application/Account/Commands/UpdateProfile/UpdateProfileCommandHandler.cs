using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Account.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UpdateProfileResult>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        UserManager<User> userManager,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UpdateProfileResult> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return new UpdateProfileResult(false, "User not found");

        // Validate names
        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length < 2 || request.FirstName.Length > 50)
            return new UpdateProfileResult(false, "First name must be between 2 and 50 characters");

        if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Length < 2 || request.LastName.Length > 50)
            return new UpdateProfileResult(false, "Last name must be between 2 and 50 characters");

        // Update names
        user.UpdateProfile(request.FirstName, request.LastName);

        // Update username if provided
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            // Validate username format
            if (request.Username.Length < 3 || request.Username.Length > 20)
                return new UpdateProfileResult(false, "Username must be between 3 and 20 characters");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_-]+$"))
                return new UpdateProfileResult(false, "Username can only contain letters, numbers, underscores, and hyphens");

            // Check if username is already taken
            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null && existingUser.Id != request.UserId)
                return new UpdateProfileResult(false, "Username is already taken");

            user.UserName = request.Username;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new UpdateProfileResult(false, $"Profile update failed: {errors}");
        }

        _logger.LogInformation("Profile updated for user {UserId}", request.UserId);

        return new UpdateProfileResult(true, "Profile updated successfully");
    }
}
