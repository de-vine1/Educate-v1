namespace Educate.Application.Models.DTOs;

public class SubjectDto
{
    public int SubjectId { get; set; }
    public int LevelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSubjectDto
{
    public int LevelId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UpdateSubjectDto
{
    public string Name { get; set; } = string.Empty;
}
