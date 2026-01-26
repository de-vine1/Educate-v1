using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Courses.Commands.SubscribeToCourse;

public class SubscribeToCourseCommandHandler : IRequestHandler<SubscribeToCourseCommand, bool>
{
    private readonly IAppDbContext _context;

    public SubscribeToCourseCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(SubscribeToCourseCommand request, CancellationToken cancellationToken)
    {
        var existingSubscription = await _context.Subscriptions.AnyAsync(s =>
            s.UserId == request.UserId && s.CourseId == request.CourseId && s.IsActive, cancellationToken
        );

        if (existingSubscription)
            return false; // Already subscribed

        var course = await _context.Courses.FindAsync(new object[] { request.CourseId }, cancellationToken);
        if (course == null)
            throw new ArgumentException("Course not found");

        var subscription = Subscription.Create(
            request.UserId,
            request.CourseId,
            course.AnnualPrice,
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1)
        );

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
