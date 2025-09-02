using Educate.Domain.Entities;

namespace Educate.Application.Interfaces;

public interface IJwtService
{
    Task<string> GenerateTokenAsync(User user);
    Task<string> GenerateRefreshTokenAsync(User user);
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    string GeneratePasswordResetToken(User user);
    bool ValidatePasswordResetToken(string token, out string userId);
}
