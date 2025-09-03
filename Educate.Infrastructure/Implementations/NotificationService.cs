using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Educate.Infrastructure.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public NotificationService(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
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

        var title = $"Subscription Expiring Soon";
        var message =
            $"Your subscription for {courseName} - {levelName} expires in {daysRemaining} day(s) on {expiryDate:MMM dd, yyyy}. Renew now to continue access.";

        // Create in-app notification
        await CreateInAppNotificationAsync(userId, "SUBSCRIPTION_EXPIRY", title, message);

        // Send email
        var renewalLink = $"https://yourdomain.com/renew"; // Replace with actual renewal URL
        var emailBody =
            $@"
            <h3>Subscription Expiry Reminder</h3>
            <p>Dear {user.FirstName},</p>
            <p>{message}</p>
            <p><a href='{renewalLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Renew Subscription</a></p>
            <p>Best regards,<br>Educate Team</p>";

        await _emailService.SendEmailAsync(user.Email!, title, emailBody);

        // Mark email as sent
        var notification = await _context
            .Notifications.Where(n =>
                n.UserId == userId && n.Type == "SUBSCRIPTION_EXPIRY" && !n.EmailSent
            )
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync();

        if (notification != null)
        {
            notification.EmailSent = true;
            await _context.SaveChangesAsync();
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

    public async Task<List<object>> GetUserNotificationsAsync(string userId)
    {
        return await _context
            .Notifications.Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.NotificationId,
                n.Type,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
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
}
