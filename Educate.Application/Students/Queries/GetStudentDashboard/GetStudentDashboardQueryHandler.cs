using Educate.Application.Interfaces;
using Educate.Application.Students.Models;
using Educate.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Students.Queries.GetStudentDashboard;

public class GetStudentDashboardQueryHandler : IRequestHandler<GetStudentDashboardQuery, StudentDashboardDto>
{
    private readonly IAppDbContext _context;
    private readonly UserManager<User> _userManager;

    public GetStudentDashboardQueryHandler(IAppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<StudentDashboardDto> Handle(GetStudentDashboardQuery request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            throw new ArgumentException("User not found");

        var userSummary = new UserSummary(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? "",
            user.StudentId
        );

        // Get subscriptions
        var now = DateTime.UtcNow;
        var subscriptions = await _context.Subscriptions
            .Where(s => s.UserId == request.UserId)
            .Include(s => s.Level)
            .ThenInclude(l => l.Course)
            .ToListAsync(cancellationToken);

        var activeSubscriptions = subscriptions
            .Where(s => s.IsCurrentlyValid())
            .Select(s => new SubscriptionSummary(
                s.Id,
                s.LevelId,
                s.Level.Name,
                s.Level.Course.Name,
                s.StartDate,
                s.EndDate,
                (s.EndDate - now).Days,
                s.CanRenew()
            ))
            .ToList();

        var expiringSubscriptions = activeSubscriptions
            .Where(s => s.DaysRemaining <= 30 && s.DaysRemaining > 0)
            .ToList();

        // Get available levels
        var subscribedLevelIds = activeSubscriptions.Select(s => s.LevelId).ToHashSet();
        
        var allLevels = await _context.Levels
            .Include(l => l.Course)
            .ToListAsync(cancellationToken);

        var availableLevels = allLevels
            .Select(l => new AvailableLevelDto(
                l.Id,
                l.Name,
                l.Course.Name,
                l.Price,
                subscribedLevelIds.Contains(l.Id)
            ))
            .ToList();

        return new StudentDashboardDto(
            userSummary,
            activeSubscriptions,
            expiringSubscriptions,
            availableLevels
        );
    }
}
