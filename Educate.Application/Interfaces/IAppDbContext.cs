using Educate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Course> Courses { get; }
    DbSet<User> Users { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
