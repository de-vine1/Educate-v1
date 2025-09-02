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

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}
