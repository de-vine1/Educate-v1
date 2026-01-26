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
    Task SendPasswordResetConfirmationAsync(
        string toEmail,
        string userName,
        string ipAddress,
        string userAgent
    );
    Task SendPasswordSetConfirmationAsync(string toEmail, string userName);
    Task SendPasswordChangedNotificationAsync(string toEmail, string userName);
    Task SendEmailVerificationCodeAsync(string toEmail, string userName, string code, string emailType);
    Task SendEmailChangedNotificationAsync(string toEmail, string userName, string newEmail);
    Task SendAccountDeletionConfirmationAsync(string toEmail, string userName);
    Task Send2FAEnabledNotificationAsync(string toEmail, string userName);
    Task Send2FADisabledNotificationAsync(string toEmail, string userName);
}
