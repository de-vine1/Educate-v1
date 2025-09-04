namespace Educate.Application.Interfaces;

public interface INotificationService
{
    Task SendPaymentSuccessNotificationAsync(string userId, string courseName, string levelName, string paymentReference, decimal amount);
    Task SendPaymentFailedNotificationAsync(string userId, string courseName, string levelName, string paymentReference);
    Task SendExpiryReminderAsync(string userId, string courseName, string levelName, DateTime expiryDate, int daysRemaining);
    Task CreateInAppNotificationAsync(string userId, string type, string title, string message);
    Task<List<object>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(string userId);
}
