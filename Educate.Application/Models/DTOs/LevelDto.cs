namespace Educate.Application.Models.DTOs;

public class LevelDto
{
    public int LevelId { get; set; }
    public int CourseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SubjectDto> Subjects { get; set; } = new();
}

public class CreateLevelDto
{
    public int CourseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateLevelDto
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}
