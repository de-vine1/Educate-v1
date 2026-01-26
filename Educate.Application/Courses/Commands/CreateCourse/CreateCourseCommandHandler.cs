using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;

namespace Educate.Application.Courses.Commands.CreateCourse;

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateCourseCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // Infer Code from Name if not provided (simple logic for now)
        var code = request.Code ?? request.Name.Substring(0, Math.Min(3, request.Name.Length)).ToUpper();
        
        var course = Course.Create(request.Name, code, request.Description);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        return course.Id;
    }
}
