using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Jobs;

public class SendWelcomeEmailsJob
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWelcomeEmailsJob> _logger;

    public SendWelcomeEmailsJob(
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<SendWelcomeEmailsJob> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Starting welcome email job");

            var users = await _userManager.Users.ToListAsync();

            if (!users.Any())
            {
                _logger.LogInformation("No users found to send emails");
                return;
            }

            var emailsSent = 0;
            foreach (var user in users)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            user.Email,
                            "Welcome to Educate",
                            "<h1>HI, welcome to Educate</h1><p>We're excited to have you on our platform!</p>"
                        );
                        emailsSent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}", user.Email);
                    }
                }
            }

            _logger.LogInformation("Welcome email job completed. Sent {Count} emails", emailsSent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in welcome email job");
            throw;
        }
    }
}
