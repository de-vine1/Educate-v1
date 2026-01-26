using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Courses.Queries.GetAllCourses;

public class GetAllCoursesQueryHandler : IRequestHandler<GetAllCoursesQuery, List<Course>>
{
    private readonly IAppDbContext _context;

    public GetAllCoursesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Course>> Handle(GetAllCoursesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Courses
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);
    }
}
