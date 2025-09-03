namespace Educate.Application.Interfaces;

public interface ISubscriptionService
{
    Task CheckExpiredSubscriptionsAsync();
    Task NotifyExpiringSubscriptionsAsync();
    Task<bool> HasActiveSubscriptionAsync(string userId, Guid courseId, Guid levelId);
    Task<IEnumerable<object>> GetUserActiveSubscriptionsAsync(string userId);
}
