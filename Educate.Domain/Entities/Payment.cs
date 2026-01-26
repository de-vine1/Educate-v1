namespace Educate.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public Guid LevelId { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty; // Paystack, Stripe, etc.
    public string TransactionId { get; private set; } = string.Empty;
    public string? EncryptedCardToken { get; private set; }
    public string Status { get; private set; } = string.Empty; // Pending, Success, Failed
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Level Level { get; set; } = null!;

    private Payment() { } // Required for EF Core

    public static Payment Create(string userId, Guid levelId, decimal amount, string paymentMethod, string transactionId, string? encryptedCardToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty.");

        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LevelId = levelId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            TransactionId = transactionId,
            EncryptedCardToken = encryptedCardToken,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsSuccess()
    {
        Status = "Success";
    }

    public void MarkAsFailed()
    {
        Status = "Failed";
    }
}
