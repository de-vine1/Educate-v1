namespace Educate.Application.Courses.Models;

public record CourseDto(
    Guid Id,
    string Name,
    string Code,
    string Description,
    bool IsActive,
    DateTime CreatedAt
);
