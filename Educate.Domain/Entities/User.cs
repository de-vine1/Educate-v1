using Microsoft.AspNetCore.Identity;

namespace Educate.Domain.Entities;

public class User : IdentityUser
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? StudentId { get; private set; }
    public string? EncryptedPersonalData { get; private set; }
    public string? SubscriptionStatus { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? EmailConfirmedAt { get; private set; }
    public string? OAuthProvider { get; private set; }
    public string? OAuthId { get; private set; }

    public static User Create(string firstName, string lastName, string email, string? username = null)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");

        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = username ?? email,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        EmailConfirmedAt = DateTime.UtcNow;
    }

    public void LinkOAuth(string provider, string providerId)
    {
        OAuthProvider = provider;
        OAuthId = providerId;
    }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
