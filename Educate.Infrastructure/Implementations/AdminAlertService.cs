using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Domain.Enums;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class AdminAlertService : IAdminAlertService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminAlertService> _logger;

    public AdminAlertService(AppDbContext context, ILogger<AdminAlertService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateAlertAsync(
        string alertType,
        string title,
        string message,
        string severity = "Medium",
        string? relatedEntityId = null,
        string? relatedEntityType = null
    )
    {
        var alert = new AdminAlert
        {
            AlertType = alertType,
            Title = title,
            Message = message,
            Severity = severity,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
        };

        _context.AdminAlerts.Add(alert);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created admin alert: {AlertType} - {Title}", alertType, title);
    }

    public async Task<IEnumerable<object>> GetUnreadAlertsAsync()
    {
        return await _context
            .AdminAlerts.Where(a => !a.IsRead)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.AlertId,
                a.AlertType,
                a.Title,
                a.Message,
                a.Severity,
                a.CreatedAt,
                a.IsResolved,
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<object>> GetAllAlertsAsync(int page = 1, int pageSize = 50)
    {
        return await _context
            .AdminAlerts.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.AlertId,
                a.AlertType,
                a.Title,
                a.Message,
                a.Severity,
                a.IsRead,
                a.IsResolved,
                a.CreatedAt,
                a.ReadAt,
                a.ResolvedAt,
            })
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(Guid alertId)
    {
        var alert = await _context.AdminAlerts.FindAsync(alertId);
        if (alert == null)
            return false;

        alert.IsRead = true;
        alert.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsResolvedAsync(Guid alertId)
    {
        var alert = await _context.AdminAlerts.FindAsync(alertId);
        if (alert == null)
            return false;

        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.UtcNow;
        if (!alert.IsRead)
        {
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CheckPaymentFailuresAsync()
    {
        var recentFailures = await _context
            .Payments.Where(p =>
                p.Status == PaymentStatus.Failed && p.CreatedAt >= DateTime.UtcNow.AddHours(-1)
            )
            .CountAsync();

        if (recentFailures > 5)
        {
            await CreateAlertAsync(
                "PaymentFailure",
                "High Payment Failure Rate",
                $"{recentFailures} payment failures in the last hour",
                "High"
            );
        }
    }

    public async Task CheckSubscriptionExpiriesAsync()
    {
        var expiringToday = await _context
            .UserCourses.Where(uc =>
                uc.SubscriptionEndDate.Date == DateTime.UtcNow.Date
                && (uc.Status == "Active" || uc.Status == "Renewed")
            )
            .CountAsync();

        if (expiringToday > 10)
        {
            await CreateAlertAsync(
                "SubscriptionExpiry",
                "High Subscription Expiry Rate",
                $"{expiringToday} subscriptions expiring today",
                "Medium"
            );
        }
    }
}
