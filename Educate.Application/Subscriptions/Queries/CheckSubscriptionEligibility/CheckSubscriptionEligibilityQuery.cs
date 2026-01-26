using Educate.Application.Subscriptions.Models;
using MediatR;

namespace Educate.Application.Subscriptions.Queries.CheckSubscriptionEligibility;

public record CheckSubscriptionEligibilityQuery(string UserId, Guid LevelId) : IRequest<SubscriptionEligibilityDto>;
