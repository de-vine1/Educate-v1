namespace Educate.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Paystack, Stripe, etc.
    public string TransactionId { get; set; } = string.Empty;
    public string? EncryptedCardToken { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Success, Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
