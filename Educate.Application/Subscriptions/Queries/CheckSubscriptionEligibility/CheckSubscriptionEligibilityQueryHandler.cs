using Educate.Application.Interfaces;
using Educate.Application.Subscriptions.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Subscriptions.Queries.CheckSubscriptionEligibility;

public class CheckSubscriptionEligibilityQueryHandler : IRequestHandler<CheckSubscriptionEligibilityQuery, SubscriptionEligibilityDto>
{
    private readonly IAppDbContext _context;

    public CheckSubscriptionEligibilityQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionEligibilityDto> Handle(CheckSubscriptionEligibilityQuery request, CancellationToken cancellationToken)
    {
        // Check if level exists
        var level = await _context.Levels.FindAsync(new object[] { request.LevelId }, cancellationToken);
        if (level == null)
        {
            return new SubscriptionEligibilityDto(
                false,
                "Level not found",
                null
            );
        }

        // Find existing subscriptions for this user and level
        var existingSubscription = await _context.Subscriptions
            .Where(s => s.UserId == request.UserId && s.LevelId == request.LevelId)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSubscription == null)
        {
            // No prior subscription - can subscribe
            return new SubscriptionEligibilityDto(
                true,
                "No existing subscription",
                null
            );
        }

        var subscriptionStatus = new SubscriptionStatus(
            existingSubscription.Id,
            existingSubscription.StartDate,
            existingSubscription.EndDate,
            existingSubscription.IsActive,
            existingSubscription.IsExpired(),
            existingSubscription.CanRenew(),
            existingSubscription.AmountPaid
        );

        // Check if currently valid (active and not expired)
        if (existingSubscription.IsCurrentlyValid())
        {
            return new SubscriptionEligibilityDto(
                false,
                "You already have an active subscription to this level",
                subscriptionStatus
            );
        }

        // Check if eligible for renewal
        if (existingSubscription.CanRenew())
        {
            return new SubscriptionEligibilityDto(
                true,
                "Subscription can be renewed",
                subscriptionStatus
            );
        }

        // Subscription existed but has expired beyond renewal period - can subscribe fresh
        if (existingSubscription.IsExpired())
        {
            return new SubscriptionEligibilityDto(
                true,
                "Previous subscription has expired. You can subscribe again.",
                subscriptionStatus
            );
        }

        // Default case - can subscribe
        return new SubscriptionEligibilityDto(
            true,
            "Eligible to subscribe",
            subscriptionStatus
        );
    }
}
