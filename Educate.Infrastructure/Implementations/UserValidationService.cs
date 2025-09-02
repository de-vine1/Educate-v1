using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace Educate.Infrastructure.Implementations;

public class UserValidationService : IUserValidationService
{
    private readonly UserManager<User> _userManager;

    public UserValidationService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> IsUsernameAvailableAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        return user == null;
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null;
    }

    public bool ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        // At least one uppercase letter
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;

        // At least one lowercase letter
        if (!Regex.IsMatch(password, @"[a-z]"))
            return false;

        // At least one digit
        if (!Regex.IsMatch(password, @"\d"))
            return false;

        return true;
    }
}