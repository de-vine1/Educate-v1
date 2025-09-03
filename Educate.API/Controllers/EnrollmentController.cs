using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;

    public EnrollmentController(
        AppDbContext context,
        IPaymentService paymentService,
        IEmailService emailService
    )
    {
        _context = context;
        _paymentService = paymentService;
        _emailService = emailService;
    }

    [HttpPost("enroll")]
    public async Task<IActionResult> Enroll([FromBody] EnrollmentRequestDto dto)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Check if already enrolled
        var existingEnrollment = await _context.UserCourses.AnyAsync(uc =>
            uc.UserId == userId
            && uc.CourseId == dto.CourseId
            && uc.LevelId == dto.LevelId
            && uc.Status == "Active"
        );

        if (existingEnrollment)
            return BadRequest(
                new EnrollmentResponseDto
                {
                    Success = false,
                    Message = "Already enrolled in this course level",
                }
            );

        // Validate course and level exist
        var level = await _context
            .Levels.Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.LevelId == dto.LevelId && l.CourseId == dto.CourseId);

        if (level == null)
            return NotFound(
                new EnrollmentResponseDto { Success = false, Message = "Course level not found" }
            );

        // Create payment record
        var payment = new Payment
        {
            UserId = userId,
            Amount = 50000, // ₦50,000 for 6-month subscription
            Provider = "Paystack",
            Reference = Guid.NewGuid().ToString(),
            Status = "Pending",
        };

        _context.Payments.Add(payment);

        // Create user course enrollment
        var userCourse = new UserCourse
        {
            UserId = userId,
            CourseId = dto.CourseId,
            LevelId = dto.LevelId,
            SubscriptionStartDate = DateTime.UtcNow,
            SubscriptionEndDate = DateTime.UtcNow.AddMonths(6),
            Status = "Pending",
            PaymentId = payment.PaymentId,
        };

        _context.UserCourses.Add(userCourse);
        await _context.SaveChangesAsync();

        // Generate payment URL (mock for now)
        var paymentUrl = $"https://paystack.com/pay/{payment.Reference}";

        return Ok(
            new EnrollmentResponseDto
            {
                Success = true,
                Message = "Enrollment created. Complete payment to activate subscription.",
                UserCourseId = userCourse.UserCourseId,
                PaymentId = payment.PaymentId,
                PaymentUrl = paymentUrl,
                Amount = payment.Amount,
            }
        );
    }

    [HttpPost("payment-callback")]
    public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackDto dto)
    {
        var payment = await _context
            .Payments.Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Reference == dto.Reference);

        if (payment == null)
            return NotFound("Payment not found");

        // Update payment status
        payment.Status = dto.Status == "success" ? "Success" : "Failed";

        if (payment.Status == "Success")
        {
            // Activate user course subscription
            var userCourse = await _context
                .UserCourses.Include(uc => uc.Course)
                .Include(uc => uc.Level)
                .FirstOrDefaultAsync(uc => uc.PaymentId == payment.PaymentId);

            if (userCourse != null)
            {
                userCourse.Status = "Active";

                // Send confirmation email
                try
                {
                    await _emailService.SendEmailAsync(
                        payment.User.Email!,
                        "Subscription Activated",
                        $"Your subscription for {userCourse.Course.Name} - {userCourse.Level.Name} has been activated. Valid until {userCourse.SubscriptionEndDate:MMM dd, yyyy}."
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the request
                    Console.WriteLine($"Failed to send confirmation email: {ex.Message}");
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { Success = payment.Status == "Success", Status = payment.Status });
    }

    [HttpGet("my-enrollments")]
    public async Task<IActionResult> GetMyEnrollments()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var enrollments = await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .Where(uc => uc.UserId == userId)
            .Select(uc => new
            {
                uc.UserCourseId,
                CourseName = uc.Course.Name,
                LevelName = uc.Level.Name,
                uc.Status,
                uc.SubscriptionStartDate,
                uc.SubscriptionEndDate,
                IsActive = uc.Status == "Active" && uc.SubscriptionEndDate > DateTime.UtcNow,
                DaysRemaining = uc.Status == "Active"
                    ? (uc.SubscriptionEndDate.Date - DateTime.UtcNow.Date).Days
                    : 0,
            })
            .OrderByDescending(uc => uc.SubscriptionStartDate)
            .ToListAsync();

        return Ok(enrollments);
    }

    [HttpPost("renew/{userCourseId}")]
    public async Task<IActionResult> RenewSubscription(Guid userCourseId)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var userCourse = await _context
            .UserCourses.Include(uc => uc.Course)
            .Include(uc => uc.Level)
            .FirstOrDefaultAsync(uc => uc.UserCourseId == userCourseId && uc.UserId == userId);

        if (userCourse == null)
            return NotFound("Subscription not found");

        // Create new payment for renewal
        var payment = new Payment
        {
            UserId = userId,
            Amount = 50000, // ₦50,000 for 6-month renewal
            Provider = "Paystack",
            Reference = Guid.NewGuid().ToString(),
            Status = "Pending",
        };

        _context.Payments.Add(payment);

        // Update subscription dates
        var newStartDate =
            userCourse.SubscriptionEndDate > DateTime.UtcNow
                ? userCourse.SubscriptionEndDate
                : DateTime.UtcNow;

        userCourse.SubscriptionEndDate = newStartDate.AddMonths(6);
        userCourse.Status = "Pending"; // Will be activated after payment
        userCourse.PaymentId = payment.PaymentId;

        await _context.SaveChangesAsync();

        var paymentUrl = $"https://paystack.com/pay/{payment.Reference}";

        return Ok(
            new EnrollmentResponseDto
            {
                Success = true,
                Message = "Renewal initiated. Complete payment to extend subscription.",
                UserCourseId = userCourse.UserCourseId,
                PaymentId = payment.PaymentId,
                PaymentUrl = paymentUrl,
                Amount = payment.Amount,
            }
        );
    }
}
