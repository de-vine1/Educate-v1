namespace Educate.Domain.Entities;

public class Test
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Duration { get; set; } // in minutes
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
}

public class TestResult
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int TestId { get; set; }
    public int Score { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Test Test { get; set; } = null!;
}
