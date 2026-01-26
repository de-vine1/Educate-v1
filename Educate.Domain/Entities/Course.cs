namespace Educate.Domain.Entities;

public class Course
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal AnnualPrice { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Course() { } // Required for EF Core

    public static Course Create(string name, string description, decimal annualPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Course name cannot be empty.");
            
        if (annualPrice < 0)
            throw new ArgumentException("Price cannot be negative.");

        return new Course
        {
            Name = name,
            Description = description,
            AnnualPrice = annualPrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative.");
            
        AnnualPrice = newPrice;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
