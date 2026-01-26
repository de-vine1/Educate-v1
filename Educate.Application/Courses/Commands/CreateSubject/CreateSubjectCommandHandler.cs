using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Courses.Commands.CreateSubject;

public class CreateSubjectCommandHandler : IRequestHandler<CreateSubjectCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateSubjectCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
    {
        var levelExists = await _context.Levels.AnyAsync(l => l.Id == request.LevelId, cancellationToken);
        if (!levelExists)
            throw new ArgumentException("Level not found");

        var subject = Subject.Create(request.Name, request.Code, request.LevelId);

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync(cancellationToken);

        return subject.Id;
    }
}
