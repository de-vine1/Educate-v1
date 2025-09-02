using System.Net;
using System.Net.Mail;
using Educate.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Educate.Infrastructure.Implementations;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public SmtpEmailService(IConfiguration configuration)
    {
        var server = configuration["Smtp:Server"]!;
        var port = int.Parse(configuration["Smtp:Port"]!);
        var username = configuration["Smtp:Username"]!;
        var password = configuration["Smtp:Password"]!;

        _senderEmail = configuration["Smtp:SenderEmail"]!;
        _senderName = configuration["Smtp:SenderName"]!;

        _smtpClient = new SmtpClient(server, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
        };
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_senderEmail, _senderName),
            Subject = subject,
            Body = message,
            IsBodyHtml = false,
        };

        mailMessage.To.Add(toEmail);

        await _smtpClient.SendMailAsync(mailMessage);
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
        var subject = "Password Reset Request";
        var message = $"Click the link to reset your password: /reset-password?token={resetToken}";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationToken)
    {
        var subject = "Confirm Your Email Address";
        var message = $"Hello {userName},\n\nPlease confirm your email address by clicking the link below:\n\n/confirm-email?token={confirmationToken}\n\nThis link will expire in 24 hours.";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendLoginNotificationAsync(string toEmail, string userName, string ipAddress, string userAgent)
    {
        var subject = "Login Notification";
        var message = $"Hello {userName},\n\nYour account was accessed from:\nIP: {ipAddress}\nDevice: {userAgent}\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nIf this wasn't you, please secure your account immediately.";

        await SendEmailAsync(toEmail, subject, message);
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}
