using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Courses.Commands.CreateLevel;

public class CreateLevelCommandHandler : IRequestHandler<CreateLevelCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateLevelCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateLevelCommand request, CancellationToken cancellationToken)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId, cancellationToken);
        if (!courseExists)
            throw new ArgumentException("Course not found");

        var level = Level.Create(request.Name, request.Order, request.Price, request.CourseId);

        _context.Levels.Add(level);
        await _context.SaveChangesAsync(cancellationToken);

        return level.Id;
    }
}
