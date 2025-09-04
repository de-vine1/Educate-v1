using Educate.Domain.Enums;
using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/admin/payment-monitoring")]
[Authorize(Policy = "AdminOnly")]
public class PaymentMonitoringController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentMonitoringController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetPaymentStats()
    {
        var stats = new
        {
            TotalPayments = await _context.Payments.CountAsync(),
            SuccessfulPayments = await _context.Payments.CountAsync(p =>
                p.Status == PaymentStatus.Success
            ),
            FailedPayments = await _context.Payments.CountAsync(p =>
                p.Status == PaymentStatus.Failed
            ),
            PendingPayments = await _context.Payments.CountAsync(p =>
                p.Status == PaymentStatus.Pending
            ),
            TotalRevenue = await _context
                .Payments.Where(p => p.Status == PaymentStatus.Success)
                .SumAsync(p => p.Amount),
            PaystackPayments = await _context.Payments.CountAsync(p =>
                p.Provider == PaymentProvider.Paystack
            ),
            MonnifyPayments = await _context.Payments.CountAsync(p =>
                p.Provider == PaymentProvider.Monnify
            ),
        };

        return Ok(stats);
    }

    [HttpGet("recent-failures")]
    public async Task<IActionResult> GetRecentFailures([FromQuery] int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);

        var failures = await _context
            .Payments.Include(p => p.User)
            .Where(p => p.Status == PaymentStatus.Failed && p.CreatedAt >= cutoffTime)
            .Select(p => new
            {
                p.PaymentId,
                p.Reference,
                p.Amount,
                p.Provider,
                p.CreatedAt,
                UserEmail = p.User.Email,
                UserName = p.User.FirstName + " " + p.User.LastName,
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(failures);
    }

    [HttpGet("daily-stats")]
    public async Task<IActionResult> GetDailyStats([FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;

        var dailyStats = await _context
            .Payments.Where(p => p.CreatedAt >= startDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalPayments = g.Count(),
                SuccessfulPayments = g.Count(p => p.Status == PaymentStatus.Success),
                FailedPayments = g.Count(p => p.Status == PaymentStatus.Failed),
                Revenue = g.Where(p => p.Status == PaymentStatus.Success).Sum(p => p.Amount),
            })
            .OrderBy(s => s.Date)
            .ToListAsync();

        return Ok(dailyStats);
    }

    [HttpGet("test-webhook/{provider}")]
    public IActionResult TestWebhook(string provider)
    {
        var testPayload = provider.ToLower() switch
        {
            "paystack" =>
                """{"event":"charge.success","data":{"reference":"TEST_REF_123","status":"success","amount":5000000}}""",
            "monnify" =>
                """{"eventType":"SUCCESSFUL_TRANSACTION","eventData":{"paymentReference":"TEST_REF_123","paymentStatus":"PAID"}}""",
            _ => null,
        };

        if (testPayload == null)
            return BadRequest("Invalid provider");

        return Ok(new { message = "Test webhook payload", payload = testPayload });
    }
}
