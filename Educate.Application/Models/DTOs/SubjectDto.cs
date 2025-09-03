namespace Educate.Application.Models.DTOs;

public class SubjectDto
{
    public Guid SubjectId { get; set; }
    public Guid LevelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSubjectDto
{
    public Guid LevelId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UpdateSubjectDto
{
    public string Name { get; set; } = string.Empty;
}
