using Educate.Domain.Entities;
using MediatR;

namespace Educate.Application.Courses.Queries.GetAllCourses;

public record GetAllCoursesQuery() : IRequest<List<Course>>;
