using Educate.Domain.Entities;
using MediatR;

namespace Educate.Application.Courses.Queries.GetCourseById;

public record GetCourseByIdQuery(int Id) : IRequest<Course?>;
