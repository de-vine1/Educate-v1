using System.Text.Json;
using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class TestService : ITestService
{
    private readonly AppDbContext _context;
    private readonly IQuestionBankService _questionBankService;
    private readonly ILogger<TestService> _logger;

    public TestService(
        AppDbContext context,
        IQuestionBankService questionBankService,
        ILogger<TestService> logger
    )
    {
        _context = context;
        _questionBankService = questionBankService;
        _logger = logger;
    }

    public async Task<object> StartPracticeTestAsync(
        string userId,
        int subjectId,
        int questionCount = 10
    )
    {
        var subject = await _context
            .Subjects.Include(s => s.Level)
            .ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId);

        if (subject == null)
            return new { Success = false, Message = "Subject not found" };

        var questions = await _questionBankService.GetRandomQuestionsAsync(
            subjectId,
            questionCount
        );
        var questionIds = questions.Select(q => ((dynamic)q).QuestionId).ToList();

        var session = new TestSession
        {
            UserId = userId,
            CourseId = subject.Level.CourseId,
            LevelId = subject.LevelId,
            SubjectId = subjectId,
            TestType = "Practice",
            Questions = JsonSerializer.Serialize(questionIds),
            TimeLimit = 0, // No time limit for practice
        };

        _context.TestSessions.Add(session);
        await _context.SaveChangesAsync();

        return new
        {
            Success = true,
            SessionId = session.SessionId,
            TestType = "Practice",
            Subject = subject.Name,
            TotalQuestions = questionIds.Count,
            TimeLimit = 0,
            Questions = questions,
        };
    }

    public async Task<object> StartMockExamAsync(string userId, int courseId, int levelId)
    {
        var level = await _context
            .Levels.Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.LevelId == levelId && l.CourseId == courseId);

        if (level == null)
            return new { Success = false, Message = "Level not found" };

        var questions = await _questionBankService.GetMockExamQuestionsAsync(
            courseId,
            levelId,
            100
        );
        var questionIds = questions.Select(q => ((dynamic)q).QuestionId).ToList();

        var session = new TestSession
        {
            UserId = userId,
            CourseId = courseId,
            LevelId = levelId,
            TestType = "Mock",
            Questions = JsonSerializer.Serialize(questionIds),
            TimeLimit = 180, // 3 hours
        };

        _context.TestSessions.Add(session);
        await _context.SaveChangesAsync();

        return new
        {
            Success = true,
            SessionId = session.SessionId,
            TestType = "Mock",
            Course = level.Course.Name,
            Level = level.Name,
            TotalQuestions = questionIds.Count,
            TimeLimit = 180,
            Questions = questions.Select(q => new
            {
                ((dynamic)q).QuestionId,
                ((dynamic)q).QuestionText,
                ((dynamic)q).Options,
            }),
        };
    }

    public async Task<object> GetTestSessionAsync(string userId, int sessionId)
    {
        var session = await _context
            .TestSessions.Include(s => s.Course)
            .Include(s => s.Level)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

        if (session == null || !session.IsActive)
            return new { Success = false, Message = "Session not found or expired" };

        var questionIds =
            JsonSerializer.Deserialize<List<int>>(session.Questions) ?? new List<int>();
        var currentAnswers = string.IsNullOrEmpty(session.CurrentAnswers)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(session.CurrentAnswers)
                ?? new Dictionary<string, string>();

        return new
        {
            Success = true,
            SessionId = session.SessionId,
            TestType = session.TestType,
            CurrentQuestionIndex = session.CurrentQuestionIndex,
            TotalQuestions = questionIds.Count,
            TimeLimit = session.TimeLimit,
            StartTime = session.StartTime,
            CurrentAnswers = currentAnswers,
            IsCompleted = session.IsCompleted,
        };
    }

    public async Task<bool> SubmitAnswerAsync(
        string userId,
        int sessionId,
        int questionId,
        string answer
    )
    {
        var session = await _context.TestSessions.FirstOrDefaultAsync(s =>
            s.SessionId == sessionId && s.UserId == userId && s.IsActive
        );

        if (session == null)
            return false;

        var currentAnswers = string.IsNullOrEmpty(session.CurrentAnswers)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(session.CurrentAnswers)
                ?? new Dictionary<string, string>();

        currentAnswers[questionId.ToString()] = answer;
        session.CurrentAnswers = JsonSerializer.Serialize(currentAnswers);
        session.CurrentQuestionIndex = Math.Max(session.CurrentQuestionIndex, currentAnswers.Count);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<object> CompleteTestAsync(string userId, int sessionId)
    {
        var session = await _context
            .TestSessions.Include(s => s.Course)
            .Include(s => s.Level)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

        if (session == null || session.IsCompleted)
            return new { Success = false, Message = "Session not found or already completed" };

        var questionIds =
            JsonSerializer.Deserialize<List<int>>(session.Questions) ?? new List<int>();
        var userAnswers = string.IsNullOrEmpty(session.CurrentAnswers)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(session.CurrentAnswers)
                ?? new Dictionary<string, string>();

        var questions = await _context
            .QuestionBanks.Where(q => questionIds.Contains(q.QuestionId))
            .ToListAsync();

        int correctAnswers = 0;
        foreach (var question in questions)
        {
            if (
                userAnswers.TryGetValue(question.QuestionId.ToString(), out var userAnswer)
                && userAnswer == question.CorrectAnswer
            )
            {
                correctAnswers++;
            }
        }

        var score = questionIds.Count > 0 ? (decimal)correctAnswers / questionIds.Count * 100 : 0;
        var timeTaken = DateTime.UtcNow - session.StartTime;

        var attempt = new UserTestAttempt
        {
            UserId = userId,
            CourseId = session.CourseId,
            LevelId = session.LevelId,
            SubjectId = session.SubjectId,
            TestType = session.TestType,
            Score = score,
            TotalQuestions = questionIds.Count,
            CorrectAnswers = correctAnswers,
            WrongAnswers = questionIds.Count - correctAnswers,
            Answers = session.CurrentAnswers,
            TimeTaken = timeTaken,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow,
        };

        session.IsCompleted = true;
        session.IsActive = false;
        session.EndTime = DateTime.UtcNow;

        _context.UserTestAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return new
        {
            Success = true,
            AttemptId = attempt.AttemptId,
            Score = score,
            CorrectAnswers = correctAnswers,
            TotalQuestions = questionIds.Count,
            TimeTaken = timeTaken,
            PassMark = 50m,
            Passed = score >= 50,
        };
    }

    public async Task<object> GetTestResultsAsync(string userId, int attemptId)
    {
        var attempt = await _context
            .UserTestAttempts.Include(a => a.Course)
            .Include(a => a.Level)
            .Include(a => a.Subject)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.UserId == userId);

        if (attempt == null)
            return new { Success = false, Message = "Test attempt not found" };

        var userAnswers =
            JsonSerializer.Deserialize<Dictionary<string, string>>(attempt.Answers)
            ?? new Dictionary<string, string>();
        var questionIds = userAnswers.Keys.Select(int.Parse).ToList();

        var questions = await _context
            .QuestionBanks.Where(q => questionIds.Contains(q.QuestionId))
            .Select(q => new
            {
                q.QuestionId,
                q.QuestionText,
                q.Options,
                q.CorrectAnswer,
                q.Explanation,
                UserAnswer = userAnswers.ContainsKey(q.QuestionId.ToString())
                    ? userAnswers[q.QuestionId.ToString()]
                    : "",
                IsCorrect = userAnswers.ContainsKey(q.QuestionId.ToString())
                    && userAnswers[q.QuestionId.ToString()] == q.CorrectAnswer,
            })
            .ToListAsync();

        return new
        {
            Success = true,
            AttemptId = attempt.AttemptId,
            TestType = attempt.TestType,
            Course = attempt.Course.Name,
            Level = attempt.Level.Name,
            Subject = attempt.Subject?.Name,
            Score = attempt.Score,
            CorrectAnswers = attempt.CorrectAnswers,
            TotalQuestions = attempt.TotalQuestions,
            TimeTaken = attempt.TimeTaken,
            AttemptDate = attempt.AttemptDate,
            Questions = questions,
        };
    }

    public async Task<IEnumerable<object>> GetUserTestHistoryAsync(string userId)
    {
        return await _context
            .UserTestAttempts.Include(a => a.Course)
            .Include(a => a.Level)
            .Include(a => a.Subject)
            .Where(a => a.UserId == userId && a.IsCompleted)
            .OrderByDescending(a => a.AttemptDate)
            .Select(a => new
            {
                a.AttemptId,
                a.TestType,
                CourseName = a.Course.Name,
                LevelName = a.Level.Name,
                SubjectName = a.Subject != null ? a.Subject.Name : "Full Exam",
                a.Score,
                a.CorrectAnswers,
                a.TotalQuestions,
                a.TimeTaken,
                a.AttemptDate,
                Passed = a.Score >= 50,
            })
            .ToListAsync();
    }

    public async Task<object> GetTestAnalyticsAsync(string userId)
    {
        var attempts = await _context
            .UserTestAttempts.Include(a => a.Subject)
            .Where(a => a.UserId == userId && a.IsCompleted)
            .ToListAsync();

        if (!attempts.Any())
            return new { TotalAttempts = 0 };

        var subjectPerformance = attempts
            .Where(a => a.Subject != null)
            .GroupBy(a => a.Subject!.Name)
            .Select(g => new
            {
                Subject = g.Key,
                AverageScore = g.Average(a => a.Score),
                BestScore = g.Max(a => a.Score),
                AttemptCount = g.Count(),
                LastAttempt = g.Max(a => a.AttemptDate),
            })
            .OrderBy(s => s.AverageScore)
            .ToList();

        return new
        {
            TotalAttempts = attempts.Count,
            AverageScore = attempts.Average(a => a.Score),
            BestScore = attempts.Max(a => a.Score),
            PassRate = attempts.Count(a => a.Score >= 50) * 100.0 / attempts.Count,
            SubjectPerformance = subjectPerformance,
            WeakestSubjects = subjectPerformance.Take(3),
            RecentImprovement = attempts.Count >= 5
                ? attempts.OrderByDescending(a => a.AttemptDate).Take(5).Average(a => a.Score)
                    - attempts.OrderBy(a => a.AttemptDate).Take(5).Average(a => a.Score)
                : 0,
        };
    }
}
