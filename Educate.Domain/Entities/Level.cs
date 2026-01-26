namespace Educate.Domain.Entities;

public class Level
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty; // e.g., "ATS 1"
    public int Order { get; private set; } // 1, 2, 3
    public decimal Price { get; private set; } // Subscription Price
    public Guid CourseId { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    public Course Course { get; private set; } = null!;
    public ICollection<Subject> Subjects { get; private set; } = new List<Subject>();

    private Level() { }

    public static Level Create(string name, int order, decimal price, Guid courseId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required");
        if (price < 0) throw new ArgumentException("Price cannot be negative");

        return new Level
        {
            Id = Guid.NewGuid(),
            Name = name,
            Order = order,
            Price = price,
            CourseId = courseId,
            IsActive = true
        };
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0) throw new ArgumentException("Price cannot be negative");
        Price = newPrice;
    }
}
