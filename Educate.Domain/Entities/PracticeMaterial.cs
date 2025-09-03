namespace Educate.Domain.Entities;

public class PracticeMaterial
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid LevelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
    public Level Level { get; set; } = null!;
}
