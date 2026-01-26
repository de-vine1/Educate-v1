using System.Net.Http.Json;
using Educate.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailService> _logger;
    private readonly string _baseUrl;

    public EmailService(HttpClient httpClient, IConfiguration configuration, ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["NodeMailer:BaseUrl"] ?? "http://localhost:4000";
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        try
        {
            var payload = new
            {
                to = toEmail,
                subject = subject,
                html = message
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/send-email", payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email via NodeMailer. Status: {Status}, Error: {Error}", response.StatusCode, error);
                // We might not want to throw here to avoid breaking the user flow if email fails, 
                // but the interface implies success. For now, log error.
                throw new HttpRequestException($"NodeMailer failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "Welcome to Educate Platform";
        var message = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Welcome to Educate Platform!</h2>
                <p>Hello {userName},</p>
                <p>Your account has been created successfully. We are excited to have you on board.</p>
                <p>Start exploring our courses today!</p>
                <br>
                <p>Best regards,</p>
                <p>Educate Platform Team</p>
            </div>";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Password Reset Request - Educate Platform";
        var resetLink = $"https://educate.com/reset-password?token={resetToken}";
        var message = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Password Reset Request</h2>
                <p>Hello,</p>
                <p>You have requested to reset your password for your Educate Platform account.</p>
                <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>Or copy this link: {resetLink}</p>
                <p>This link will expire in 30 minutes for security reasons.</p>
                <p>If you did not request this password reset, please ignore this email.</p>
                <br>
                <p>Best regards,</p>
                <p>Educate Platform Team</p>
            </div>";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationToken)
    {
        var subject = "Confirm Your Email Address";
        var confirmLink = $"https://educate.com/confirm-email?token={confirmationToken}";
        var message = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Confirm Your Email</h2>
                <p>Hello {userName},</p>
                <p>Please confirm your email address by clicking the link below:</p>
                <p><a href='{confirmLink}' style='background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Confirm Email</a></p>
                <p>This link will expire in 24 hours.</p>
                <br>
                <p>Best regards,</p>
                <p>Educate Platform Team</p>
            </div>";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendLoginNotificationAsync(string toEmail, string userName, string ipAddress, string userAgent)
    {
        var subject = "Login Notification";
        var message = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>New Login Detected</h2>
                <p>Hello {userName},</p>
                <p>Your account was accessed from:</p>
                <ul>
                    <li><strong>IP:</strong> {ipAddress}</li>
                    <li><strong>Device:</strong> {userAgent}</li>
                    <li><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                </ul>
                <p>If this wasn't you, please secure your account immediately.</p>
                <br>
                <p>Best regards,</p>
                <p>Educate Platform Team</p>
            </div>";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendPasswordResetConfirmationAsync(string toEmail, string userName, string ipAddress, string userAgent)
    {
        var subject = "Password Reset Successful - Educate Platform";
        var message = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Password Reset Successful</h2>
                <p>Hello {userName},</p>
                <p>Your password has been successfully reset.</p>
                <p><strong>Reset Details:</strong></p>
                <ul>
                    <li><strong>IP:</strong> {ipAddress}</li>
                    <li><strong>Device:</strong> {userAgent}</li>
                    <li><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                </ul>
                <p>If you did not perform this action, please contact support immediately.</p>
                <br>
                <p>Best regards,</p>
                <p>Educate Platform Team</p>
            </div>";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendPasswordSetConfirmationAsync(string toEmail, string userName)
    {
        var subject = "Account Setup Complete - Educate Platform";
        var loginLink = "https://educate.com/login";
        var message = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2>Account Setup Complete</h2>
                <p>Hello {userName},</p>
                <p>Great news! Your account setup is now complete.</p>
                <p>You can now login using either your email/password or Google OAuth.</p>
                <p><a href='{loginLink}'>Login here</a></p>
                <br>
                <p>Best regards,</p>
                <p>Educate Platform Team</p>
            </div>";

        await SendEmailAsync(toEmail, subject, message);
    }
}
