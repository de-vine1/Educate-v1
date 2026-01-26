using Educate.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Jobs;

public class DeactivateExpiredSubscriptionsJob
{
    private readonly IAppDbContext _context;
    private readonly ILogger<DeactivateExpiredSubscriptionsJob> _logger;

    public DeactivateExpiredSubscriptionsJob(IAppDbContext context, ILogger<DeactivateExpiredSubscriptionsJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Starting expired subscriptions deactivation job");

            var now = DateTime.UtcNow;
            var expiredSubscriptions = await _context.Subscriptions
                .Where(s => s.IsActive && s.EndDate < now)
                .ToListAsync();

            if (!expiredSubscriptions.Any())
            {
                _logger.LogInformation("No expired subscriptions found");
                return;
            }

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Deactivate();
            }

            await _context.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation(
                "Deactivated {Count} expired subscriptions", 
                expiredSubscriptions.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating expired subscriptions");
            throw;
        }
    }
}
