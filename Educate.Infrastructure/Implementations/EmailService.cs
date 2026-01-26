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

    private async Task<string> GetEmailTemplateAsync(string templateName, Dictionary<string, string> placeholders)
    {
        try
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "templates", "email");
            var layoutPath = Path.Combine(templatePath, "layout.html");
            var contentPath = Path.Combine(templatePath, $"{templateName}.html");

            // Look for templates in common locations if AppContext.BaseDirectory doesn't have them
            // This fallback is useful during development vs published
            if (!File.Exists(layoutPath))
            {
                // Fallback attempt for dev environment
                var currentDir = Directory.GetCurrentDirectory();
                 // Try looking in educate.api/wwwroot if running from there
                templatePath = Path.Combine(currentDir, "wwwroot", "templates", "email");
                layoutPath = Path.Combine(templatePath, "layout.html");
                contentPath = Path.Combine(templatePath, $"{templateName}.html");
                
                if (!File.Exists(layoutPath))
                {
                    // one more level up if we are in bin folder
                     templatePath = Path.Combine(currentDir, "..", "..", "..", "wwwroot", "templates", "email");
                     layoutPath = Path.Combine(templatePath, "layout.html");
                     contentPath = Path.Combine(templatePath, $"{templateName}.html");
                }
            }

            if (!File.Exists(layoutPath) || !File.Exists(contentPath))
            {
                _logger.LogWarning("Template not found. Layout: {Layout}, Content: {Content}", layoutPath, contentPath);
                // Fallback to simple replacements if file not found, or throw
                return $"<html><body><p>Error: Email template '{templateName}' not found.</p></body></html>";
            }

            var layoutContent = await File.ReadAllTextAsync(layoutPath);
            var innerContent = await File.ReadAllTextAsync(contentPath);

            // 1. Inject content into layout
            var fullHtml = layoutContent.Replace("{{content}}", innerContent);

            // 2. Add global placeholders
            placeholders["year"] = DateTime.UtcNow.Year.ToString();
            placeholders["platformName"] = "Educate Platform";
            
            // 3. Replace all placeholders
            foreach (var kvp in placeholders)
            {
                fullHtml = fullHtml.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            return fullHtml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load email template {TemplateName}", templateName);
            throw; // Or return basic HTML
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName }
        };
        var message = await GetEmailTemplateAsync("welcome", placeholders);
        await SendEmailAsync(toEmail, "Welcome to Educate Platform", message);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var actionUrl = $"https://educate.com/reset-password?token={resetToken}";
        var placeholders = new Dictionary<string, string>
        {
            { "actionUrl", actionUrl }
        };
        var message = await GetEmailTemplateAsync("password-reset", placeholders);
        await SendEmailAsync(toEmail, "Password Reset Request - Educate Platform", message);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationToken)
    {
        var actionUrl = $"https://educate.com/confirm-email?token={confirmationToken}";
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName },
            { "actionUrl", actionUrl }
        };
        var message = await GetEmailTemplateAsync("confirm-email", placeholders);
        await SendEmailAsync(toEmail, "Confirm Your Email Address", message);
    }

    public async Task SendLoginNotificationAsync(string toEmail, string userName, string ipAddress, string userAgent)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName },
            { "ipAddress", ipAddress },
            { "userAgent", userAgent },
            { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC" }
        };
        var message = await GetEmailTemplateAsync("login-notification", placeholders);
        await SendEmailAsync(toEmail, "Login Notification", message);
    }

    public async Task SendPasswordResetConfirmationAsync(string toEmail, string userName, string ipAddress, string userAgent)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName },
            { "ipAddress", ipAddress },
            { "userAgent", userAgent },
            { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC" }
        };
        var message = await GetEmailTemplateAsync("password-reset-success", placeholders);
        await SendEmailAsync(toEmail, "Password Reset Successful - Educate Platform", message);
    }

    public async Task SendPasswordSetConfirmationAsync(string toEmail, string userName)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName }
        };
        var message = await GetEmailTemplateAsync("account-setup-complete", placeholders);
        await SendEmailAsync(toEmail, "Account Setup Complete - Educate Platform", message);
    }

    public async Task SendPasswordChangedNotificationAsync(string toEmail, string userName)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName }
        };
        var message = await GetEmailTemplateAsync("password-changed", placeholders);
        await SendEmailAsync(toEmail, "Password Changed Successfully", message);
    }

    public async Task SendEmailVerificationCodeAsync(string toEmail, string userName, string code, string emailType)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName },
            { "code", code },
            { "emailType", emailType }
        };
        var message = await GetEmailTemplateAsync("email-verification-code", placeholders);
        await SendEmailAsync(toEmail, "Email Verification Code", message);
    }

    public async Task SendEmailChangedNotificationAsync(string toEmail, string userName, string newEmail)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName },
            { "newEmail", newEmail }
        };
        var message = await GetEmailTemplateAsync("email-changed", placeholders);
        await SendEmailAsync(toEmail, "Email Address Changed", message);
    }

    public async Task SendAccountDeletionConfirmationAsync(string toEmail, string userName)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "userName", userName }
        };
        var message = await GetEmailTemplateAsync("account-deleted", placeholders);
        await SendEmailAsync(toEmail, "Account Deleted", message);
    }

    public async Task Send2FAEnabledNotificationAsync(string toEmail, string userName)
    {
         var placeholders = new Dictionary<string, string>
        {
            { "userName", userName }
        };
        var message = await GetEmailTemplateAsync("2fa-enabled", placeholders);
        await SendEmailAsync(toEmail, "Two-Factor Authentication Enabled", message);
    }

    public async Task Send2FADisabledNotificationAsync(string toEmail, string userName)
    {
         var placeholders = new Dictionary<string, string>
        {
            { "userName", userName }
        };
        var message = await GetEmailTemplateAsync("2fa-disabled", placeholders);
        await SendEmailAsync(toEmail, "Two-Factor Authentication Disabled", message);
    }
}
