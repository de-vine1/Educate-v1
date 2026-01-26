namespace Educate.Application.Students.Models;

public record StudentDashboardDto(
    UserSummary User,
    List<SubscriptionSummary> ActiveSubscriptions,
    List<SubscriptionSummary> ExpiringSubscriptions,
    List<AvailableLevelDto> AvailableLevels
);

public record UserSummary(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string? StudentId
);

public record SubscriptionSummary(
    Guid SubscriptionId,
    Guid LevelId,
    string LevelName,
    string CourseName,
    DateTime StartDate,
    DateTime EndDate,
    int DaysRemaining,
    bool CanRenew
);

public record AvailableLevelDto(
    Guid LevelId,
    string LevelName,
    string CourseName,
    decimal Price,
    bool HasActiveSubscription
);
