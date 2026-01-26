using Educate.Application.Common.Models;
using Educate.Application.Courses.Models;
using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Courses.Queries.GetAllCourses;

public class GetAllCoursesQueryHandler : IRequestHandler<GetAllCoursesQuery, PaginatedResult<CourseDto>>
{
    private readonly IAppDbContext _context;

    public GetAllCoursesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<CourseDto>> Handle(GetAllCoursesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseDto(
                c.Id,
                c.Name,
                c.Code,
                c.Description,
                c.IsActive,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<CourseDto>(items, totalCount, request.Page, request.PageSize);
    }
}
