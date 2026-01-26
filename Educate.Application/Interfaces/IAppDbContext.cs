using Educate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Educate.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Course> Courses { get; }
    DbSet<Level> Levels { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<User> Users { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    
    // Assessment Engine
    DbSet<Question> Questions { get; }
    DbSet<Exam> Exams { get; }
    DbSet<ExamQuestion> ExamQuestions { get; }
    DbSet<StudentExam> StudentExams { get; }
    DbSet<StudentAnswer> StudentAnswers { get; }
    
    // Two-Factor Authentication
    DbSet<TwoFactorBackupCode> TwoFactorBackupCodes { get; }
    
    // Login History
    DbSet<LoginAttempt> LoginAttempts { get; }

    // User Preferences
    DbSet<UserSettings> UserSettings { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
