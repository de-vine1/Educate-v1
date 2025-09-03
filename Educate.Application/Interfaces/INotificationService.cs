namespace Educate.Application.Interfaces;

public interface INotificationService
{
    Task SendExpiryReminderAsync(
        string userId,
        string courseName,
        string levelName,
        DateTime expiryDate,
        int daysRemaining
    );
    Task CreateInAppNotificationAsync(string userId, string type, string title, string message);
    Task<List<object>> GetUserNotificationsAsync(string userId);
    Task MarkAsReadAsync(Guid notificationId);
}
