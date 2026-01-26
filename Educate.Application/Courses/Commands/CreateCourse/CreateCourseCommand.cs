using MediatR;

namespace Educate.Application.Courses.Commands.CreateCourse;

public record CreateCourseCommand(string Name, string Description, string? Code = null) : IRequest<Guid>;
