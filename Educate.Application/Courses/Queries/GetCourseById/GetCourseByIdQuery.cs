using Educate.Domain.Entities;
using MediatR;

namespace Educate.Application.Courses.Queries.GetCourseById;

public record GetCourseByIdQuery(Guid Id) : IRequest<Course?>;
