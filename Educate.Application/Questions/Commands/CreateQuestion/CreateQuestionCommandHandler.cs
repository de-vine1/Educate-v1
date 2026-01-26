using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;

namespace Educate.Application.Questions.Commands.CreateQuestion;

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateQuestionCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        // Validate subject exists
        var subject = await _context.Subjects.FindAsync(new object[] { request.SubjectId }, cancellationToken);
        if (subject == null)
            throw new ArgumentException("Subject not found");

        var question = Question.Create(
            request.SubjectId,
            request.QuestionText,
            request.QuestionType,
            request.DifficultyLevel,
            request.Points,
            request.Options,
            request.CorrectAnswer
        );

        _context.Questions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        return question.Id;
    }
}
