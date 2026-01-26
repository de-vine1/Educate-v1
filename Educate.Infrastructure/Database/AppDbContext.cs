using Educate.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Educate.Application.Interfaces;

namespace Educate.Infrastructure.Database;

public class AppDbContext : IdentityDbContext<User>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Level> Levels => Set<Level>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public new DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PracticeMaterial> PracticeMaterials { get; set; } = null!;
    public DbSet<Test> Tests { get; set; } = null!;
    public DbSet<TestResult> TestResults { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    
    // Assessment Engine
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<StudentExam> StudentExams => Set<StudentExam>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    
    // Two-Factor Authentication
    public DbSet<TwoFactorBackupCode> TwoFactorBackupCodes => Set<TwoFactorBackupCode>();
    
    // Login History
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    // User Preferences
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Static GUIDs for idempotency
        var atsId = Guid.Parse("d7d2d3a0-1b2c-4d3e-8f5a-2b3c4d5e6f7a");
        var icanId = Guid.Parse("e8e3d4b1-2c3d-5e4f-9a6b-3c4d5e6f7a8b");

        // ATS Levels
        var ats1Id = Guid.Parse("a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5");
        var ats2Id = Guid.Parse("a2a2a2a2-b3b3-c4c4-d5d5-e6e6e6e6e6e6");
        var ats3Id = Guid.Parse("a3a3a3a3-b4b4-c5c5-d6d6-e7e7e7e7e7e7");

        // ICAN Levels
        var foundationId = Guid.Parse("f1f1f1f1-a2a2-b3b3-c4c4-d5d5d5d5d5d5");
        var skillsId = Guid.Parse("b2b2b2b2-b3b3-c4c4-d5d5-e6e6e6e6e6e6");
        var proId = Guid.Parse("c3c3c3c3-c4c4-d5d5-e6e6-f7f7f7f7f7f7");

        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Course>().HasData(
            new { Id = atsId, Name = "ATS", Code = "ATS", Description = "Association of Accountancy Bodies in West Africa - Accounting Technicians Scheme", IsActive = true, CreatedAt = createdAt },
            new { Id = icanId, Name = "ICAN", Code = "ICAN", Description = "Institute of Chartered Accountants of Nigeria", IsActive = true, CreatedAt = createdAt }
        );

        modelBuilder.Entity<Level>().HasData(
            // ATS
            new { Id = ats1Id, Name = "ATS 1", Order = 1, Price = 15000m, CourseId = atsId, IsActive = true },
            new { Id = ats2Id, Name = "ATS 2", Order = 2, Price = 20000m, CourseId = atsId, IsActive = true },
            new { Id = ats3Id, Name = "ATS 3", Order = 3, Price = 25000m, CourseId = atsId, IsActive = true },
            
            // ICAN
            new { Id = foundationId, Name = "Foundation", Order = 1, Price = 40000m, CourseId = icanId, IsActive = true },
            new { Id = skillsId, Name = "Skills", Order = 2, Price = 55000m, CourseId = icanId, IsActive = true },
            new { Id = proId, Name = "Professional", Order = 3, Price = 70000m, CourseId = icanId, IsActive = true }
        );

        modelBuilder.Entity<Subject>().HasData(
            // ATS 1
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Name = "Basic Accounting Processes & Systems", Code = "BAPS", LevelId = ats1Id, IsActive = true },
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Name = "Economics", Code = "ECO", LevelId = ats1Id, IsActive = true },
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Name = "Business Law", Code = "LAW", LevelId = ats1Id, IsActive = true },
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Name = "Communication Skills", Code = "COM", LevelId = ats1Id, IsActive = true },

            // ATS 2
            new { Id = Guid.Parse("22222222-2222-2222-2222-222222222201"), Name = "Principles of Auditing", Code = "AUD", LevelId = ats2Id, IsActive = true },
            new { Id = Guid.Parse("22222222-2222-2222-2222-222222222202"), Name = "Public Sector Accounting", Code = "PSA", LevelId = ats2Id, IsActive = true },
            new { Id = Guid.Parse("22222222-2222-2222-2222-222222222203"), Name = "Quantitative Analysis", Code = "QTA", LevelId = ats2Id, IsActive = true },
            new { Id = Guid.Parse("22222222-2222-2222-2222-222222222204"), Name = "Information Technology", Code = "IT", LevelId = ats2Id, IsActive = true },

            // ATS 3
            new { Id = Guid.Parse("33333333-3333-3333-3333-333333333301"), Name = "Principles of Taxation", Code = "TAX", LevelId = ats3Id, IsActive = true },
            new { Id = Guid.Parse("33333333-3333-3333-3333-333333333302"), Name = "Cost Accounting", Code = "COST", LevelId = ats3Id, IsActive = true },
            new { Id = Guid.Parse("33333333-3333-3333-3333-333333333303"), Name = "Preparing Tax Computations & Returns", Code = "PTCR", LevelId = ats3Id, IsActive = true },
            new { Id = Guid.Parse("33333333-3333-3333-3333-333333333304"), Name = "Management", Code = "MGT", LevelId = ats3Id, IsActive = true },

            // ICAN Foundation
            new { Id = Guid.Parse("44444444-4444-4444-4444-444444444401"), Name = "Financial Accounting", Code = "FA", LevelId = foundationId, IsActive = true },
            new { Id = Guid.Parse("44444444-4444-4444-4444-444444444402"), Name = "Business Law", Code = "BL", LevelId = foundationId, IsActive = true },
            new { Id = Guid.Parse("44444444-4444-4444-4444-444444444403"), Name = "Management Information", Code = "MI", LevelId = foundationId, IsActive = true },

            // ICAN Skills
            new { Id = Guid.Parse("55555555-5555-5555-5555-555555555501"), Name = "Financial Reporting", Code = "FR", LevelId = skillsId, IsActive = true },
            new { Id = Guid.Parse("55555555-5555-5555-5555-555555555502"), Name = "Audit & Assurance", Code = "AA", LevelId = skillsId, IsActive = true },
            new { Id = Guid.Parse("55555555-5555-5555-5555-555555555503"), Name = "Taxation", Code = "TX", LevelId = skillsId, IsActive = true },

            // ICAN Professional
            new { Id = Guid.Parse("66666666-6666-6666-6666-666666666601"), Name = "Corporate Reporting", Code = "CR", LevelId = proId, IsActive = true },
            new { Id = Guid.Parse("66666666-6666-6666-6666-666666666602"), Name = "Advanced Audit & Assurance", Code = "AAA", LevelId = proId, IsActive = true },
            new { Id = Guid.Parse("66666666-6666-6666-6666-666666666603"), Name = "Strategic Financial Management", Code = "SFM", LevelId = proId, IsActive = true }
        );
    }
}
