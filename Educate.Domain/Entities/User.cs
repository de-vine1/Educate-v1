using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Educate.Domain.Entities;

public class User : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? StudentId { get; set; }

    public string? EncryptedPersonalData { get; set; }

    [MaxLength(20)]
    public string? SubscriptionStatus { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? EmailConfirmedAt { get; set; }

    [MaxLength(50)]
    public string? OAuthProvider { get; set; }

    [MaxLength(100)]
    public string? OAuthId { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
