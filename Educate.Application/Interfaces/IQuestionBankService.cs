namespace Educate.Application.Interfaces;

public interface IQuestionBankService
{
    Task<bool> AddQuestionAsync(
        int courseId,
        int levelId,
        int subjectId,
        string questionText,
        string options,
        string correctAnswer,
        string explanation,
        string createdBy
    );
    Task<bool> UpdateQuestionAsync(
        int questionId,
        string questionText,
        string options,
        string correctAnswer,
        string explanation
    );
    Task<bool> ToggleQuestionStatusAsync(int questionId);
    Task<IEnumerable<object>> GetQuestionsAsync(
        int? courseId = null,
        int? levelId = null,
        int? subjectId = null
    );
    Task<IEnumerable<object>> GetRandomQuestionsAsync(int subjectId, int count);
    Task<IEnumerable<object>> GetMockExamQuestionsAsync(int courseId, int levelId, int count);
}
