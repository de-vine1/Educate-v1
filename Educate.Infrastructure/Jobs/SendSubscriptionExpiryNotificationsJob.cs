using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Jobs;

public class SendSubscriptionExpiryNotificationsJob
{
    private readonly IAppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendSubscriptionExpiryNotificationsJob> _logger;

    public SendSubscriptionExpiryNotificationsJob(
        IAppDbContext context,
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<SendSubscriptionExpiryNotificationsJob> logger)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Starting subscription expiry notification job");

            var now = DateTime.UtcNow;
            var expiryThreshold = now.AddDays(7); // Notify 7 days before expiry

            // Find subscriptions expiring within 7 days
            var expiringSubscriptions = await _context.Subscriptions
                .Include(s => s.Level)
                .Where(s => s.IsActive && s.EndDate > now && s.EndDate <= expiryThreshold)
                .ToListAsync();

            if (!expiringSubscriptions.Any())
            {
                _logger.LogInformation("No expiring subscriptions found");
                return;
            }

            var emailsSent = 0;
            foreach (var subscription in expiringSubscriptions)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(subscription.UserId);
                    if (user == null || string.IsNullOrEmpty(user.Email))
                        continue;

                    var daysUntilExpiry = (subscription.EndDate - now).Days;
                    var levelName = subscription.Level?.Name ?? "your level";
                    var userName = !string.IsNullOrEmpty(user.FirstName) 
                        ? $"{user.FirstName} {user.LastName}" 
                        : "Student";

                    var subject = "Your Subscription is Expiring Soon";
                    var body = $@"
                        <h2>Dear {userName},</h2>
                        <p>Your subscription to <strong>{levelName}</strong> is expiring in <strong>{daysUntilExpiry} days</strong>.</p>
                        <p><strong>Expiry Date:</strong> {subscription.EndDate:MMMM dd, yyyy}</p>
                        <p>Renew now to continue accessing your study materials and maintain your learning progress.</p>
                        <p>Best regards,<br/>The Educate Team</p>
                    ";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    emailsSent++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send expiry notification for subscription {SubscriptionId}", subscription.Id);
                }
            }

            _logger.LogInformation(
                "Subscription expiry notification job completed. Sent {Count} notifications for {Total} expiring subscriptions",
                emailsSent,
                expiringSubscriptions.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in subscription expiry notification job");
            throw;
        }
    }
}
