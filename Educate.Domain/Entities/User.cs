using Microsoft.AspNetCore.Identity;

namespace Educate.Domain.Entities;

public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? EncryptedPersonalData { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
