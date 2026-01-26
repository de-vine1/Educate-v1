using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;

namespace Educate.Application.Courses.Commands.CreateCourse;

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, int>
{
    private readonly IAppDbContext _context;

    public CreateCourseCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = Course.Create(request.Name, request.Description, request.AnnualPrice);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        return course.Id;
    }
}
