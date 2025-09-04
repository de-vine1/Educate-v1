namespace Educate.Application.Interfaces;

public interface IStudentManagementService
{
    Task<IEnumerable<object>> GetAllStudentsAsync(
        string? searchTerm = null,
        string? courseFilter = null,
        string? statusFilter = null
    );
    Task<object> GetStudentDetailsAsync(string userId);
    Task<bool> ExtendSubscriptionAsync(
        string userId,
        Guid courseId,
        Guid levelId,
        int months,
        string adminId
    );
    Task<bool> ToggleScholarshipAsync(string userId, Guid courseId, Guid levelId, string adminId);
    Task<bool> ResetTestAttemptsAsync(string userId, Guid? courseId = null, string adminId = "");
    Task<object> SendAnnouncementAsync(
        string title,
        string message,
        string? targetCourse = null,
        string? targetLevel = null,
        string? targetUserId = null,
        string adminId = ""
    );
}
