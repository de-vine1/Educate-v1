using MediatR;

namespace Educate.Application.Courses.Commands.CreateSubject;

public record CreateSubjectCommand(Guid LevelId, string Name, string Code) : IRequest<Guid>;
