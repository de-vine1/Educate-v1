namespace Educate.Application.Interfaces;

public interface ITestService
{
    Task<object> StartPracticeTestAsync(string userId, int subjectId, int questionCount = 10);
    Task<object> StartMockExamAsync(string userId, int courseId, int levelId);
    Task<object> GetTestSessionAsync(string userId, int sessionId);
    Task<bool> SubmitAnswerAsync(string userId, int sessionId, int questionId, string answer);
    Task<object> CompleteTestAsync(string userId, int sessionId);
    Task<object> GetTestResultsAsync(string userId, int attemptId);
    Task<IEnumerable<object>> GetUserTestHistoryAsync(string userId);
    Task<object> GetTestAnalyticsAsync(string userId);
}
