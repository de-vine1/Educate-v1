using MediatR;

namespace Educate.Application.Courses.Commands.CreateLevel;

public record CreateLevelCommand(Guid CourseId, string Name, int Order, decimal Price) : IRequest<Guid>;
