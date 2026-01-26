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
    public bool IsDeleted { get; private set; } = false;
    public DateTime? DeletedAt { get; private set; }
    
    // Two-Factor Authentication
    public bool IsTwoFactorEnabled { get; private set; } = false;
    public string? TwoFactorSecretKey { get; private set; }

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

    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required");
        
        FirstName = firstName;
        LastName = lastName;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        
        // Anonymize email for GDPR compliance
        Email = $"deleted_user_{Id}@deleted.com";
        NormalizedEmail = Email.ToUpper();
        UserName = Email;
        NormalizedUserName = Email.ToUpper();
        
        // Anonymize personal data
        FirstName = "Deleted";
        LastName = "User";
        OAuthId = null;
        StudentId = null;
    }

    public void Enable2FA(string secretKey)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("Secret key is required");
        
        IsTwoFactorEnabled = true;
        TwoFactorSecretKey = secretKey;
    }

    public void Disable2FA()
    {
        IsTwoFactorEnabled = false;
        TwoFactorSecretKey = null;
    }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    public UserSettings Settings { get; set; } = null!;
}
