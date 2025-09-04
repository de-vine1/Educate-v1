namespace Educate.Application.Interfaces;

public interface ITestService
{
    Task<object> StartPracticeTestAsync(string userId, Guid subjectId, int questionCount = 10);
    Task<object> StartMockExamAsync(string userId, Guid courseId, Guid levelId);
    Task<object> GetTestSessionAsync(string userId, Guid sessionId);
    Task<bool> SubmitAnswerAsync(string userId, Guid sessionId, Guid questionId, string answer);
    Task<object> CompleteTestAsync(string userId, Guid sessionId);
    Task<object> GetTestResultsAsync(string userId, Guid attemptId);
    Task<IEnumerable<object>> GetUserTestHistoryAsync(string userId);
    Task<object> GetTestAnalyticsAsync(string userId);
}
