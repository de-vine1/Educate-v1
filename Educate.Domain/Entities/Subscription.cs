namespace Educate.Domain.Entities;

public class Subscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal AmountPaid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
