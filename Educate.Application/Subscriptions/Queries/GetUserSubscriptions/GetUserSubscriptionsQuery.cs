using Educate.Domain.Entities;
using MediatR;

namespace Educate.Application.Subscriptions.Queries.GetUserSubscriptions;

public record GetUserSubscriptionsQuery(string UserId) : IRequest<List<SubscriptionDto>>;

public record SubscriptionDto(
    Guid Id, 
    string CourseName, 
    string LevelName, 
    DateTime StartDate, 
    DateTime EndDate, 
    decimal AmountPaid, 
    bool IsActive,
    int DaysRemaining,
    string Status,
    bool CanRenew
);
