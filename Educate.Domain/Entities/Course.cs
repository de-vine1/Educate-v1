namespace Educate.Domain.Entities;

public class Course
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty; // e.g., "ATS", "ICAN"
    public string Code { get; private set; } = string.Empty; // e.g., "ATS"
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Course() { } // Required for EF Core

    public static Course Create(string name, string code, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Course name cannot be empty.");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Course code cannot be empty.");

        return new Course
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }
    
    public ICollection<Level> Levels { get; private set; } = new List<Level>();
}
