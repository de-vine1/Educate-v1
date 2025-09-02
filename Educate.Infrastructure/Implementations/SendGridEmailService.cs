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
        var subject = "Password Reset Request";
        var message = $"Click the link to reset your password: /reset-password?token={resetToken}";

        await SendEmailAsync(toEmail, subject, message);
    }
}
