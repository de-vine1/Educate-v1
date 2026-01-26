namespace Educate.Domain.Entities;

public class Subscription
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid LevelId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal AmountPaid { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Level Level { get; set; } = null!;

    private Subscription() { } // Required for EF Core

    public static Subscription Create(string userId, Guid levelId, decimal amountPaid, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty.");

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        if (amountPaid < 0)
            throw new ArgumentException("Amount paid cannot be negative.");

        return new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LevelId = levelId,
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

    public bool IsExpired() => DateTime.UtcNow > EndDate;

    public bool IsValidAt(DateTime date) => date >= StartDate && date <= EndDate && IsActive;

    public bool IsCurrentlyValid() => IsValidAt(DateTime.UtcNow);

    public bool CanRenew()
    {
        // Can renew within 30 days before expiry or if already expired but less than 7 days ago
        if (!IsActive) return false;
        
        var gracePeriodStart = EndDate.AddDays(-30);
        var gracePeriodEnd = EndDate.AddDays(7);
        var now = DateTime.UtcNow;
        
        return now >= gracePeriodStart && now <= gracePeriodEnd;
    }

    public void Renew(int months, decimal amountPaid)
    {
        if (!CanRenew())
            throw new InvalidOperationException("Subscription is not eligible for renewal.");

        // Extend from EndDate if not expired, otherwise from today
        var renewalStart = DateTime.UtcNow > EndDate ? DateTime.UtcNow : EndDate;
        EndDate = renewalStart.AddMonths(months);
        AmountPaid += amountPaid;
        IsActive = true;
    }
}
