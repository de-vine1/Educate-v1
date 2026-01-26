namespace Educate.Application.Subscriptions.Models;

public record SubscriptionEligibilityDto(
    bool CanSubscribe,
    string Reason,
    SubscriptionStatus? ExistingSubscription
);

public record SubscriptionStatus(
    Guid SubscriptionId,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    bool IsExpired,
    bool CanRenew,
    decimal AmountPaid
);
