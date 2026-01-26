using Educate.Application.Common.Models;
using Educate.Application.Courses.Models;
using MediatR;

namespace Educate.Application.Courses.Queries.GetAllCourses;

public record GetAllCoursesQuery(int Page = 1, int PageSize = 10) : IRequest<PaginatedResult<CourseDto>>;
