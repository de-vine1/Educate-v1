using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Exams.Commands.CreateExam;

public class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateExamCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateExamCommand request, CancellationToken cancellationToken)
    {
        // Validate subject
        var subject = await _context.Subjects.FindAsync(new object[] { request.SubjectId }, cancellationToken);
        if (subject == null)
            throw new ArgumentException("Subject not found");

        // Validate questions exist and calculate total points
        var questions = await _context.Questions
            .Where(q => request.QuestionIds.Contains(q.Id))
            .ToListAsync(cancellationToken);

        if (questions.Count != request.QuestionIds.Count)
            throw new ArgumentException("One or more questions not found");

        var totalPoints = questions.Sum(q => q.Points);

        // Create exam
        var exam = Exam.Create(request.Title, request.SubjectId, request.Duration, request.PassingScore);
        exam.SetTotalPoints(totalPoints);

        _context.Exams.Add(exam);

        // Add questions to exam
        var orderNumber = 1;
        foreach (var questionId in request.QuestionIds)
        {
            var examQuestion = ExamQuestion.Create(exam.Id, questionId, orderNumber++);
            _context.ExamQuestions.Add(examQuestion);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return exam.Id;
    }
}
