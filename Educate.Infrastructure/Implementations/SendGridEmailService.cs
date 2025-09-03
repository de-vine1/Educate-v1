using Educate.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Educate.Infrastructure.Implementations;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public SendGridEmailService(ISendGridClient sendGridClient, IConfiguration configuration)
    {
        _sendGridClient = sendGridClient;
        _senderEmail = configuration["SendGrid:SenderEmail"]!;
        _senderName = configuration["SendGrid:SenderName"]!;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var from = new EmailAddress(_senderEmail, _senderName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);

        var response = await _sendGridClient.SendEmailAsync(msg);

        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var responseBody = await response.Body.ReadAsStringAsync();
            throw new Exception($"SendGrid failed: {response.StatusCode} - {responseBody}");
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "Welcome to Educate Platform";
        var message =
            $"Hello {userName},\n\nWelcome to Educate Platform! Your account has been created successfully.";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Password Reset Request - Educate Platform";
        var resetLink = $"https://educate.com/reset-password?token={resetToken}";

        var message =
            $@"Hello,

            You have requested to reset your password for your Educate Platform account.

            Click the link below to reset your password:
            {resetLink}

            This link will expire in 30 minutes for security reasons.

            If you did not request this password reset, please ignore this email and contact our support team immediately.

            Support Contact: support@educate.com

            Best regards,
            Educate Platform Team";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendEmailConfirmationAsync(
        string toEmail,
        string userName,
        string confirmationToken
    )
    {
        var subject = "Confirm Your Email Address";
        var message =
            $"Hello {userName},\n\nPlease confirm your email address by clicking the link below:\n\n/confirm-email?token={confirmationToken}\n\nThis link will expire in 24 hours.";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendLoginNotificationAsync(
        string toEmail,
        string userName,
        string ipAddress,
        string userAgent
    )
    {
        var subject = "Login Notification";
        var message =
            $"Hello {userName},\n\nYour account was accessed from:\nIP: {ipAddress}\nDevice: {userAgent}\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nIf this wasn't you, please secure your account immediately.";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendPasswordResetConfirmationAsync(
        string toEmail,
        string userName,
        string ipAddress,
        string userAgent
    )
    {
        var subject = "Password Reset Successful - Educate Platform";
        var message =
            $"Hello {userName},\n\nYour password has been successfully reset.\n\nReset Details:\nIP Address: {ipAddress}\nDevice: {userAgent}\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nIf you did not perform this action, please contact support immediately.\n\nSupport: support@educate.com\n\nBest regards,\nEducate Platform Team";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendPasswordSetConfirmationAsync(string toEmail, string userName)
    {
        var subject = "Account Setup Complete - Educate Platform";
        var loginLink = "https://educate.com/login";
        var message =
            $"Hello {userName},\n\nGreat news! Your account setup is now complete.\n\nYou have successfully set your password and can now login using either:\n• Your email and password\n• Google OAuth\n\nLogin here: {loginLink}\n\nWelcome to the Educate Platform!\n\nBest regards,\nEducate Platform Team";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendEmailWithAttachmentAsync(
        string to,
        string subject,
        string body,
        byte[] attachment,
        string fileName
    )
    {
        var from = new EmailAddress(_senderEmail, _senderName);
        var toEmail = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, body, body);

        var attachmentBase64 = Convert.ToBase64String(attachment);
        msg.AddAttachment(fileName, attachmentBase64, "application/pdf");

        var response = await _sendGridClient.SendEmailAsync(msg);

        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var responseBody = await response.Body.ReadAsStringAsync();
            throw new Exception($"SendGrid failed: {response.StatusCode} - {responseBody}");
        }
    }
}
