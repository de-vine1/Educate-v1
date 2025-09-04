namespace Educate.Application.Interfaces;

public interface ISubscriptionService
{
    Task CheckExpiredSubscriptionsAsync();
    Task NotifyExpiringSubscriptionsAsync();
    Task<bool> HasActiveSubscriptionAsync(string userId, int courseId, int levelId);
    Task<IEnumerable<object>> GetUserActiveSubscriptionsAsync(string userId);
    Task<bool> RenewSubscriptionAsync(
        string userId,
        int courseId,
        int levelId,
        string paymentReference
    );
    Task UpdateSubscriptionStatusesAsync();
    Task<IEnumerable<object>> GetAllSubscriptionsAsync();
    Task<IEnumerable<object>> GetUserRenewalHistoryAsync(string userId);
    Task<bool> HasActiveSubscriptionAsync(string userId, int subjectId);
    Task<bool> HasActiveSubscriptionForLevelAsync(string userId, int courseId, int levelId);
}
