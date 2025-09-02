using Educate.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Educate.Infrastructure.Database;

public class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<PracticeMaterial> PracticeMaterials { get; set; } = null!;
    public DbSet<Test> Tests { get; set; } = null!;
    public DbSet<TestResult> TestResults { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
