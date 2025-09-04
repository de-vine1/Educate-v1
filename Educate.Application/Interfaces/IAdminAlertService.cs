namespace Educate.Application.Interfaces;

public interface IAdminAlertService
{
    Task CreateAlertAsync(
        string alertType,
        string title,
        string message,
        string severity = "Medium",
        string? relatedEntityId = null,
        string? relatedEntityType = null
    );
    Task<IEnumerable<object>> GetUnreadAlertsAsync();
    Task<IEnumerable<object>> GetAllAlertsAsync(int page = 1, int pageSize = 50);
    Task<bool> MarkAsReadAsync(Guid alertId);
    Task<bool> MarkAsResolvedAsync(Guid alertId);
    Task CheckPaymentFailuresAsync();
    Task CheckSubscriptionExpiriesAsync();
}
