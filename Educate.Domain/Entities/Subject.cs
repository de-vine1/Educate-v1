namespace Educate.Domain.Entities;

public class Subject
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty; // e.g., "ACC101"
    public Guid LevelId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Level Level { get; private set; } = null!;

    private Subject() { }

    public static Subject Create(string name, string code, Guid levelId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required");
        
        return new Subject
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            LevelId = levelId,
            IsActive = true
        };
    }
}
