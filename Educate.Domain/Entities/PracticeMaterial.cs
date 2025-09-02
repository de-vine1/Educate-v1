namespace Educate.Domain.Entities;

public class PracticeMaterial
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
}
