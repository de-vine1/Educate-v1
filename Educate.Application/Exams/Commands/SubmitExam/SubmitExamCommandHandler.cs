using Educate.Application.Exams.Models;
using Educate.Application.Interfaces;
using Educate.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Exams.Commands.SubmitExam;

public class SubmitExamCommandHandler : IRequestHandler<SubmitExamCommand, ExamResultDto>
{
    private readonly IAppDbContext _context;

    public SubmitExamCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ExamResultDto> Handle(SubmitExamCommand request, CancellationToken cancellationToken)
    {
        var studentExam = await _context.StudentExams
            .Include(se => se.Exam)
            .Include(se => se.StudentAnswers)
            .FirstOrDefaultAsync(se => se.Id == request.StudentExamId, cancellationToken);

        if (studentExam == null || studentExam.Status != ExamStatus.InProgress)
            throw new InvalidOperationException("Exam not found or not in progress");

        // Calculate total score (only from graded answers)
        var totalScore = studentExam.StudentAnswers
            .Where(sa => sa.IsCorrect.HasValue) // Only MCQs are graded
            .Sum(sa => sa.PointsEarned);

        // Get total number of questions
        var totalQuestions = await _context.ExamQuestions
            .CountAsync(eq => eq.ExamId == studentExam.ExamId, cancellationToken);

        // Submit exam
        studentExam.Submit(totalScore, studentExam.Exam.PassingScore);
        await _context.SaveChangesAsync(cancellationToken);

        return new ExamResultDto(
            studentExam.Id,
            studentExam.Exam.Title,
            totalScore,
            studentExam.Exam.TotalPoints,
            studentExam.IsPassed ?? false,
            studentExam.StartTime,
            studentExam.EndTime ?? DateTime.UtcNow,
            studentExam.StudentAnswers.Count,
            totalQuestions
        );
    }
}
