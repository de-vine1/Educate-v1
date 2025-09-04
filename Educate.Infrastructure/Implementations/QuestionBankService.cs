using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class QuestionBankService : IQuestionBankService
{
    private readonly AppDbContext _context;
    private readonly ILogger<QuestionBankService> _logger;

    public QuestionBankService(AppDbContext context, ILogger<QuestionBankService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> AddQuestionAsync(
        int courseId,
        int levelId,
        int subjectId,
        string questionText,
        string options,
        string correctAnswer,
        string explanation,
        string createdBy
    )
    {
        var question = new QuestionBank
        {
            CourseId = courseId,
            LevelId = levelId,
            SubjectId = subjectId,
            QuestionText = questionText,
            Options = options,
            CorrectAnswer = correctAnswer,
            Explanation = explanation,

            CreatedBy = createdBy,
        };

        _context.QuestionBanks.Add(question);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateQuestionAsync(
        int questionId,
        string questionText,
        string options,
        string correctAnswer,
        string explanation
    )
    {
        var question = await _context.QuestionBanks.FindAsync(questionId);
        if (question == null)
            return false;

        question.QuestionText = questionText;
        question.Options = options;
        question.CorrectAnswer = correctAnswer;
        question.Explanation = explanation;

        question.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleQuestionStatusAsync(int questionId)
    {
        var question = await _context.QuestionBanks.FindAsync(questionId);
        if (question == null)
            return false;

        question.IsActive = !question.IsActive;
        question.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<object>> GetQuestionsAsync(
        int? courseId = null,
        int? levelId = null,
        int? subjectId = null
    )
    {
        var query = _context
            .QuestionBanks.Include(q => q.Course)
            .Include(q => q.Level)
            .Include(q => q.Subject)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(q => q.CourseId == courseId);

        if (levelId.HasValue)
            query = query.Where(q => q.LevelId == levelId);

        if (subjectId.HasValue)
            query = query.Where(q => q.SubjectId == subjectId);

        return await query
            .Select(q => new
            {
                q.QuestionId,
                q.QuestionText,
                q.Options,
                q.CorrectAnswer,
                q.Explanation,
                q.IsActive,
                CourseName = q.Course.Name,
                LevelName = q.Level.Name,
                SubjectName = q.Subject.Name,
                q.CreatedBy,
                q.CreatedAt,
            })
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<object>> GetRandomQuestionsAsync(int subjectId, int count)
    {
        var questions = await _context
            .QuestionBanks.Where(q => q.SubjectId == subjectId && q.IsActive)
            .OrderBy(q => Guid.NewGuid()) // Random ordering
            .Take(count)
            .Select(q => new
            {
                q.QuestionId,
                q.QuestionText,
                q.Options,
            })
            .ToListAsync();

        return questions;
    }

    public async Task<IEnumerable<object>> GetMockExamQuestionsAsync(
        int courseId,
        int levelId,
        int count
    )
    {
        var questions = await _context
            .QuestionBanks.Where(q => q.CourseId == courseId && q.LevelId == levelId && q.IsActive)
            .OrderBy(q => Guid.NewGuid()) // Random ordering
            .Take(count)
            .Select(q => new
            {
                q.QuestionId,
                q.QuestionText,
                q.Options,
                SubjectName = q.Subject.Name,
            })
            .ToListAsync();

        return questions;
    }
}
