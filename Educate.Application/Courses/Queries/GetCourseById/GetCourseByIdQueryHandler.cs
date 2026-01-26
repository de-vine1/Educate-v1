using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Courses.Queries.GetCourseById;

public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, Course?>
{
    private readonly IAppDbContext _context;

    public GetCourseByIdQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Course?> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
    }
}
