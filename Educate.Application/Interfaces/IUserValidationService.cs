namespace Educate.Application.Interfaces;

public interface IUserValidationService
{
    Task<bool> IsUsernameAvailableAsync(string username);
    Task<bool> IsEmailAvailableAsync(string email);
    bool ValidatePasswordComplexity(string password);
}
