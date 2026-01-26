using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Exams.Commands.SubmitAnswer;

public class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand, bool>
{
    private readonly IAppDbContext _context;

    public SubmitAnswerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        // Validate student exam exists and is in progress
        var studentExam = await _context.StudentExams
            .FirstOrDefaultAsync(se => se.Id == request.StudentExamId, cancellationToken);

        if (studentExam == null || studentExam.Status != ExamStatus.InProgress)
            throw new InvalidOperationException("Exam not found or not in progress");

        // Get question
        var question = await _context.Questions.FindAsync(new object[] { request.QuestionId }, cancellationToken);
        if (question == null)
            throw new ArgumentException("Question not found");

        // Check if answer already exists (update scenario)
        var existingAnswer = await _context.StudentAnswers
            .FirstOrDefaultAsync(sa => 
                sa.StudentExamId == request.StudentExamId && 
                sa.QuestionId == request.QuestionId, 
                cancellationToken);

        if (existingAnswer != null)
        {
            // Update existing answer
            _context.StudentAnswers.Remove(existingAnswer);
        }

        // Create new answer
        var answer = StudentAnswer.Create(request.StudentExamId, request.QuestionId, request.AnswerText);

        // Auto-grade if multiple choice
        var wasGraded = false;
        if (question.QuestionType == QuestionType.MultipleChoice)
        {
            var isCorrect = question.CorrectAnswer?.Trim().Equals(request.AnswerText.Trim(), StringComparison.OrdinalIgnoreCase) == true;
            answer.Grade(isCorrect, question.Points);
            wasGraded = true;
        }

        _context.StudentAnswers.Add(answer);
        await _context.SaveChangesAsync(cancellationToken);

        return wasGraded;
    }
}
