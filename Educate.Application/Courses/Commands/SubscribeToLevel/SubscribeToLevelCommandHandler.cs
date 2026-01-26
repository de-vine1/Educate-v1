using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Courses.Commands.SubscribeToLevel;

public class SubscribeToLevelCommandHandler : IRequestHandler<SubscribeToLevelCommand, bool>
{
    private readonly IAppDbContext _context;

    public SubscribeToLevelCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(SubscribeToLevelCommand request, CancellationToken cancellationToken)
    {
        var existingSubscription = await _context.Subscriptions.AnyAsync(s =>
            s.UserId == request.UserId && s.LevelId == request.LevelId && s.IsActive, cancellationToken
        );

        if (existingSubscription)
            return false; // Already subscribed

        var level = await _context.Levels.FindAsync(new object[] { request.LevelId }, cancellationToken);
        if (level == null)
            throw new ArgumentException("Level not found");

        var subscription = Subscription.Create(
            request.UserId,
            request.LevelId,
            level.Price,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(6) // Bi-annual subscription
        );

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
