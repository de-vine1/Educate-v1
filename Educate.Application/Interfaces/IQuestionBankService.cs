namespace Educate.Application.Interfaces;

public interface IQuestionBankService
{
    Task<bool> AddQuestionAsync(
        Guid courseId,
        Guid levelId,
        Guid subjectId,
        string questionText,
        string options,
        string correctAnswer,
        string explanation,
        string difficulty,
        string createdBy
    );
    Task<bool> UpdateQuestionAsync(
        Guid questionId,
        string questionText,
        string options,
        string correctAnswer,
        string explanation,
        string difficulty
    );
    Task<bool> ToggleQuestionStatusAsync(Guid questionId);
    Task<IEnumerable<object>> GetQuestionsAsync(
        Guid? courseId = null,
        Guid? levelId = null,
        Guid? subjectId = null,
        string? difficulty = null
    );
    Task<IEnumerable<object>> GetRandomQuestionsAsync(Guid subjectId, int count);
    Task<IEnumerable<object>> GetMockExamQuestionsAsync(Guid courseId, Guid levelId, int count);
}
