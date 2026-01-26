namespace Educate.Domain.Entities;

public class LoginAttempt
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;

    public static LoginAttempt CreateSuccessful(string userId, string ipAddress, string userAgent, string? location = null)
    {
        return new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Location = location,
            IsSuccessful = true,
            Timestamp = DateTime.UtcNow
        };
    }

    public static LoginAttempt CreateFailed(string userId, string ipAddress, string userAgent, string failureReason, string? location = null)
    {
        return new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Location = location,
            IsSuccessful = false,
            FailureReason = failureReason,
            Timestamp = DateTime.UtcNow
        };
    }
}
