namespace Educate.Application.Interfaces;

public interface IAnalyticsService
{
    Task<object> GetDashboardMetricsAsync();
    Task<object> GetRevenueAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<object> GetUserEngagementMetricsAsync();
    Task<object> GetCoursePerformanceAsync();
    Task<object> GetTestAnalyticsAsync();
    Task<IEnumerable<object>> GetTopPerformingStudentsAsync(int count = 10);
}
