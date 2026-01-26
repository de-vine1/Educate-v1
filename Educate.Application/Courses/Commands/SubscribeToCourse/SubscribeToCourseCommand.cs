using MediatR;

namespace Educate.Application.Courses.Commands.SubscribeToCourse;

public record SubscribeToCourseCommand(int CourseId, string UserId) : IRequest<bool>;
