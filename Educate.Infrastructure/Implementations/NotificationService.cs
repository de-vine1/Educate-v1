using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context,
        IEmailService emailService,
        ILogger<NotificationService> logger
    )
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendPaymentSuccessNotificationAsync(
        string userId,
        string courseName,
        string levelName,
        string paymentReference,
        decimal amount
    )
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return;

        var title = "Payment Successful!";
        var message =
            $"Your payment for {courseName} - {levelName} has been confirmed. Amount: ₦{amount:N2}. Reference: {paymentReference}";

        // Create in-app notification
        await CreateInAppNotificationAsync(userId, "PAYMENT_SUCCESS", title, message);

        // Send email with receipt
        var emailBody =
            $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #28a745;'>🎉 Payment Successful!</h2>
                <p>Dear {user.FirstName},</p>
                <p>Your subscription payment has been successfully processed.</p>
                
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                    <h3>Payment Details</h3>
                    <p><strong>Course:</strong> {courseName}</p>
                    <p><strong>Level:</strong> {levelName}</p>
                    <p><strong>Amount:</strong> ₦{amount:N2}</p>
                    <p><strong>Reference:</strong> {paymentReference}</p>
                    <p><strong>Subscription Duration:</strong> 6 months</p>
                </div>

                <p>You can now access all materials and practice tests for this level.</p>
                <p><a href='https://yourdomain.com/dashboard' style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>Go to Dashboard</a></p>
                
                <p>Best regards,<br>Educate Team</p>
            </div>";

        try
        {
            await _emailService.SendEmailAsync(user.Email!, title, emailBody);
            await MarkEmailAsSentAsync(userId, "PAYMENT_SUCCESS");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment success email to user {UserId}", userId);
        }
    }

    public async Task SendPaymentFailedNotificationAsync(
        string userId,
        string courseName,
        string levelName,
        string paymentReference
    )
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return;

        var title = "Payment Failed";
        var message =
            $"Your payment for {courseName} - {levelName} was unsuccessful. Reference: {paymentReference}. Please try again.";

        // Create in-app notification
        await CreateInAppNotificationAsync(userId, "PAYMENT_FAILED", title, message);

        // Send retry instructions email
        var retryUrl = $"https://yourdomain.com/enroll?course={courseName}&level={levelName}";
        var emailBody =
            $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #dc3545;'>❌ Payment Failed</h2>
                <p>Dear {user.FirstName},</p>
                <p>Unfortunately, your payment for <strong>{courseName} - {levelName}</strong> could not be processed.</p>
                
                <div style='background-color: #f8d7da; color: #721c24; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                    <strong>Reference:</strong> {paymentReference}
                </div>

                <h3>What you can do:</h3>
                <ul>
                    <li>Check your payment method and try again</li>
                    <li>Contact your bank if the issue persists</li>
                    <li>Try using a different payment provider</li>
                </ul>

                <p><a href='{retryUrl}' style='background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;'>Retry Payment</a></p>
                
                <p>If you continue experiencing issues, please contact our support team.</p>
                <p>Best regards,<br>Educate Team</p>
            </div>";

        try
        {
            await _emailService.SendEmailAsync(user.Email!, title, emailBody);
            await MarkEmailAsSentAsync(userId, "PAYMENT_FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment failed email to user {UserId}", userId);
        }
    }

    public async Task SendExpiryReminderAsync(
        string userId,
        string courseName,
        string levelName,
        DateTime expiryDate,
        int daysRemaining
    )
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return;

        var title =
            daysRemaining <= 1
                ? "Subscription Expires Today!"
                : $"Subscription Expiring in {daysRemaining} Days";
        var message =
            $"Your {courseName} - {levelName} subscription expires on {expiryDate:MMM dd, yyyy}. Renew now to continue access.";

        // Create in-app notification
        await CreateInAppNotificationAsync(userId, "SUBSCRIPTION_EXPIRY", title, message);

        // Send renewal email
        var renewalUrl = $"https://yourdomain.com/renew?course={courseName}&level={levelName}";
        var urgencyColor = daysRemaining <= 1 ? "#dc3545" : "#ffc107";

        var emailBody =
            $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: {urgencyColor};'>⏰ {title}</h2>
                <p>Dear {user.FirstName},</p>
                <p>Your subscription for <strong>{courseName} - {levelName}</strong> is about to expire.</p>
                
                <div style='background-color: #fff3cd; color: #856404; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid {urgencyColor};'>
                    <h3>Subscription Details</h3>
                    <p><strong>Course:</strong> {courseName}</p>
                    <p><strong>Level:</strong> {levelName}</p>
                    <p><strong>Expires:</strong> {expiryDate:MMM dd, yyyy}</p>
                    <p><strong>Days Remaining:</strong> {daysRemaining}</p>
                </div>

                <p><strong>Don't lose access!</strong> Renew now to continue enjoying:</p>
                <ul>
                    <li>Practice tests and mock exams</li>
                    <li>Study materials and resources</li>
                    <li>Progress tracking</li>
                    <li>Expert support</li>
                </ul>

                <p><a href='{renewalUrl}' style='background-color: #28a745; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Renew Subscription</a></p>
                
                <p>Best regards,<br>Educate Team</p>
            </div>";

        try
        {
            await _emailService.SendEmailAsync(user.Email!, title, emailBody);
            await MarkEmailAsSentAsync(userId, "SUBSCRIPTION_EXPIRY");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expiry reminder email to user {UserId}", userId);
        }
    }

    public async Task CreateInAppNotificationAsync(
        string userId,
        string type,
        string title,
        string message
    )
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<object>> GetUserNotificationsAsync(
        string userId,
        bool unreadOnly = false
    )
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.NotificationId,
                n.Type,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt),
            })
            .Cast<object>()
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context
            .Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    private async Task MarkEmailAsSentAsync(string userId, string type)
    {
        var notification = await _context
            .Notifications.Where(n => n.UserId == userId && n.Type == type && !n.EmailSent)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync();

        if (notification != null)
        {
            notification.EmailSent = true;
            await _context.SaveChangesAsync();
        }
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        return timeSpan.TotalDays switch
        {
            >= 1 => $"{(int)timeSpan.TotalDays} day(s) ago",
            _ => timeSpan.TotalHours switch
            {
                >= 1 => $"{(int)timeSpan.TotalHours} hour(s) ago",
                _ => $"{(int)timeSpan.TotalMinutes} minute(s) ago",
            },
        };
    }
}
