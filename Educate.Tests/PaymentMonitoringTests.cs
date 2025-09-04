using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Educate.Tests;

public class PaymentMonitoringTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger> _mockLogger;

    public PaymentMonitoringTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public async Task PaymentStats_ShouldCalculateCorrectMetrics()
    {
        // Arrange - Create test payment data
        var payments = new List<Payment>
        {
            new()
            {
                PaymentId = Guid.NewGuid(),
                Status = "Success",
                Amount = 50000,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new()
            {
                PaymentId = Guid.NewGuid(),
                Status = "Success",
                Amount = 50000,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
            },
            new()
            {
                PaymentId = Guid.NewGuid(),
                Status = "Failed",
                Amount = 50000,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new()
            {
                PaymentId = Guid.NewGuid(),
                Status = "Pending",
                Amount = 50000,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
            },
        };

        _context.Payments.AddRange(payments);
        await _context.SaveChangesAsync();

        // Act - Calculate payment statistics
        var totalPayments = await _context.Payments.CountAsync();
        var successfulPayments = await _context.Payments.CountAsync(p => p.Status == "Success");
        var failedPayments = await _context.Payments.CountAsync(p => p.Status == "Failed");
        var pendingPayments = await _context.Payments.CountAsync(p => p.Status == "Pending");
        var totalRevenue = await _context
            .Payments.Where(p => p.Status == "Success")
            .SumAsync(p => p.Amount);
        var successRate = (double)successfulPayments / totalPayments * 100;

        // Assert
        Assert.Equal(4, totalPayments);
        Assert.Equal(2, successfulPayments);
        Assert.Equal(1, failedPayments);
        Assert.Equal(1, pendingPayments);
        Assert.Equal(100000, totalRevenue);
        Assert.Equal(50.0, successRate);
    }

    [Fact]
    public async Task PaymentsByProvider_ShouldGroupCorrectly()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new()
            {
                PaymentId = Guid.NewGuid(),
                Provider = "Paystack",
                Status = "Success",
                Amount = 50000,
            },
            new()
            {
                PaymentId = Guid.NewGuid(),
                Provider = "Paystack",
                Status = "Failed",
                Amount = 50000,
            },
            new()
            {
                PaymentId = Guid.NewGuid(),
                Provider = "Monnify",
                Status = "Success",
                Amount = 50000,
            },
            new()
            {
                PaymentId = Guid.NewGuid(),
                Provider = "Monnify",
                Status = "Success",
                Amount = 50000,
            },
        };

        _context.Payments.AddRange(payments);
        await _context.SaveChangesAsync();

        // Act
        var paymentsByProvider = await _context
            .Payments.GroupBy(p => p.Provider)
            .Select(g => new
            {
                Provider = g.Key,
                TotalCount = g.Count(),
                SuccessCount = g.Count(p => p.Status == "Success"),
                SuccessRate = (double)g.Count(p => p.Status == "Success") / g.Count() * 100,
            })
            .ToListAsync();

        // Assert
        var paystackStats = paymentsByProvider.First(p => p.Provider == "Paystack");
        var monnifyStats = paymentsByProvider.First(p => p.Provider == "Monnify");

        Assert.Equal(2, paystackStats.TotalCount);
        Assert.Equal(1, paystackStats.SuccessCount);
        Assert.Equal(50.0, paystackStats.SuccessRate);

        Assert.Equal(2, monnifyStats.TotalCount);
        Assert.Equal(2, monnifyStats.SuccessCount);
        Assert.Equal(100.0, monnifyStats.SuccessRate);
    }

    [Fact]
    public async Task FailedPaymentAnalysis_ShouldIdentifyPatterns()
    {
        // Arrange
        var failedPayments = new List<Payment>
        {
            new()
            {
                PaymentId = Guid.NewGuid(),
                Status = "Failed",
                Provider = "Paystack",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            },
            new()
            {
                PaymentId = Guid.NewGuid(),
                Status = "Failed",
                Provider = "Paystack",
                CreatedAt = DateTime.UtcNow.AddMinutes(-25),
            },
            new()
            {
                PaymentId = Guid.NewGuid(),
                Status = "Failed",
                Provider = "Monnify",
                CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            },
        };

        _context.Payments.AddRange(failedPayments);
        await _context.SaveChangesAsync();

        // Act - Analyze failed payments in last hour
        var recentFailed = await _context
            .Payments.Where(p =>
                p.Status == "Failed" && p.CreatedAt >= DateTime.UtcNow.AddHours(-1)
            )
            .GroupBy(p => p.Provider)
            .Select(g => new
            {
                Provider = g.Key,
                FailureCount = g.Count(),
                LatestFailure = g.Max(p => p.CreatedAt),
            })
            .ToListAsync();

        // Assert
        Assert.Equal(2, recentFailed.Count);
        Assert.Contains(recentFailed, f => f.Provider == "Paystack" && f.FailureCount == 2);
        Assert.Contains(recentFailed, f => f.Provider == "Monnify" && f.FailureCount == 1);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}