namespace Educate.Domain.Entities;

public class Subscription
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public int CourseId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal AmountPaid { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;

    private Subscription() { } // Required for EF Core

    public static Subscription Create(string userId, int courseId, decimal amountPaid, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty.");

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        if (amountPaid < 0)
            throw new ArgumentException("Amount paid cannot be negative.");

        return new Subscription
        {
            UserId = userId,
            CourseId = courseId,
            AmountPaid = amountPaid,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
