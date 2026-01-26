using Educate.Application.Exams.Models;
using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Exams.Commands.StartExam;

public class StartExamCommandHandler : IRequestHandler<StartExamCommand, ExamAttemptDto>
{
    private readonly IAppDbContext _context;

    public StartExamCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ExamAttemptDto> Handle(StartExamCommand request, CancellationToken cancellationToken)
    {
        // Validate exam exists and is active
        var exam = await _context.Exams
            .Include(e => e.Subject)
            .ThenInclude(s => s.Level)
            .FirstOrDefaultAsync(e => e.Id == request.ExamId, cancellationToken);

        if (exam == null || !exam.IsActive)
            throw new ArgumentException("Exam not found or not active");

        // Check student has active subscription to the level containing this subject
        var hasSubscription = await _context.Subscriptions
            .AnyAsync(s => 
                s.UserId == request.UserId && 
                s.LevelId == exam.Subject.LevelId && 
                s.IsActive &&
                s.EndDate > DateTime.UtcNow, 
                cancellationToken);

        if (!hasSubscription)
            throw new UnauthorizedAccessException("You must have an active subscription to take this exam");

        // Check for existing in-progress attempt
        var existingAttempt = await _context.StudentExams
            .FirstOrDefaultAsync(se => 
                se.UserId == request.UserId && 
                se.ExamId == request.ExamId && 
                se.Status == ExamStatus.InProgress, 
                cancellationToken);

        if (existingAttempt != null)
            throw new InvalidOperationException("You already have an in-progress attempt for this exam");

        // Create new attempt
        var studentExam = StudentExam.Create(request.ExamId, request.UserId);
        _context.StudentExams.Add(studentExam);
        await _context.SaveChangesAsync(cancellationToken);

        // Get questions (without correct answers for students)
        var questions = await _context.ExamQuestions
            .Where(eq => eq.ExamId == request.ExamId)
            .Include(eq => eq.Question)
            .OrderBy(eq => eq.OrderNumber)
            .Select(eq => new ExamQuestionDto(
                eq.Question.Id,
                eq.Question.QuestionText,
                eq.Question.QuestionType,
                eq.Question.Options,
                eq.Question.Points,
                eq.OrderNumber
            ))
            .ToListAsync(cancellationToken);

        return new ExamAttemptDto(
            studentExam.Id,
            exam.Id,
            exam.Title,
            exam.Duration,
            studentExam.StartTime,
            questions
        );
    }
}
