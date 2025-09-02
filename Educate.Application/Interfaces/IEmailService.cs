namespace Educate.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string message);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
    Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationToken);
    Task SendLoginNotificationAsync(
        string toEmail,
        string userName,
        string ipAddress,
        string userAgent
    );
}
