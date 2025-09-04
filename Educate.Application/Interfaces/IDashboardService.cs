namespace Educate.Application.Interfaces;

public interface IDashboardService
{
    Task<object> GetDashboardDataAsync(string userId);
    Task<IEnumerable<object>> GetUserSubscriptionsAsync(string userId);
    Task<object> GetProgressSummaryAsync(string userId);
    Task<IEnumerable<object>> GetPaymentHistoryAsync(string userId);
    Task<bool> UpdateProgressAsync(
        string userId,
        int subjectId,
        string status,
        decimal? score = null
    );
}
