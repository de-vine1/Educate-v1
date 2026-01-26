using MediatR;

namespace Educate.Application.Courses.Commands.SubscribeToLevel;

public record SubscribeToLevelCommand(Guid LevelId, string UserId) : IRequest<bool>;
