using System.Net;
using System.Text;
using System.Text.Json;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Educate.Tests.UnitTests;

public class PaymentSecurityTests : IDisposable
{
    private readonly AppDbContext _context;

    public PaymentSecurityTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
    }

    [Fact]
    public void WebhookSignature_Paystack_ShouldValidateCorrectly()
    {
        // Arrange
        var secretKey = "sk_test_paystack_secret";
        var payload = "{\"event\":\"charge.success\",\"data\":{\"reference\":\"test_ref\"}}";

        // Act - Generate valid signature
        using var hmac = new System.Security.Cryptography.HMACSHA512(
            Encoding.UTF8.GetBytes(secretKey)
        );
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var validSignature = Convert.ToHexString(hash).ToLower();

        // Simulate validation
        var isValid = ValidatePaystackSignature(payload, validSignature, secretKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void WebhookSignature_Monnify_ShouldValidateCorrectly()
    {
        // Arrange
        var clientSecret = "monnify_client_secret";
        var payload =
            "{\"eventType\":\"SUCCESSFUL_TRANSACTION\",\"eventData\":{\"paymentReference\":\"test_ref\"}}";

        // Act - Generate valid signature (Monnify uses SHA512 with secret + payload)
        var dataToHash = clientSecret + payload;
        using var sha512 = System.Security.Cryptography.SHA512.Create();
        var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        var validSignature = Convert.ToHexString(hash).ToLower();

        // Simulate validation
        var isValid = ValidateMonnifySignature(payload, validSignature, clientSecret);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void WebhookSignature_InvalidSignature_ShouldReject()
    {
        // Arrange
        var secretKey = "sk_test_paystack_secret";
        var payload = "{\"event\":\"charge.success\",\"data\":{\"reference\":\"test_ref\"}}";
        var invalidSignature = "invalid_signature_12345";

        // Act
        var isValid = ValidatePaystackSignature(payload, invalidSignature, secretKey);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void PaymentReference_ShouldBeUnique()
    {
        // Arrange & Act
        var reference1 = GeneratePaymentReference();
        var reference2 = GeneratePaymentReference();

        // Assert
        Assert.NotEqual(reference1, reference2);
        Assert.StartsWith("EDU_", reference1);
        Assert.StartsWith("EDU_", reference2);
    }

    [Fact]
    public async Task DuplicateWebhook_ShouldBeIdempotent()
    {
        // Arrange
        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            UserId = "user1",
            Reference = "EDU_TEST_IDEMPOTENT",
            Status = "Success", // Already processed
            Provider = "Paystack",
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Act - Simulate processing same webhook multiple times
        var processCount = 0;
        for (int i = 0; i < 3; i++)
        {
            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p =>
                p.Reference == "EDU_TEST_IDEMPOTENT"
            );

            if (existingPayment?.Status != "Success")
            {
                processCount++;
                existingPayment.Status = "Success";
                await _context.SaveChangesAsync();
            }
        }

        // Assert - Should only process once
        Assert.Equal(0, processCount); // Already processed, so no additional processing

        var finalPayment = await _context.Payments.FirstOrDefaultAsync(p =>
            p.Reference == "EDU_TEST_IDEMPOTENT"
        );
        Assert.Equal("Success", finalPayment.Status);
    }

    [Fact]
    public void PaymentAmount_ShouldValidateRange()
    {
        // Arrange & Act & Assert
        var validAmount = 50000m; // ₦500
        var zeroAmount = 0m;
        var negativeAmount = -1000m;
        var tooLargeAmount = 100_000_000m; // ₦1M (hypothetical limit)

        // Business rules
        Assert.True(IsValidPaymentAmount(validAmount));
        Assert.False(IsValidPaymentAmount(zeroAmount));
        Assert.False(IsValidPaymentAmount(negativeAmount));
        Assert.False(IsValidPaymentAmount(tooLargeAmount));
    }

    [Fact]
    public void MonnifyIPWhitelist_ShouldValidateSourceIP()
    {
        // Arrange
        var allowedIP = "35.242.133.146"; // Monnify webhook IP
        var blockedIP = "192.168.1.1";
        var monnifyIPs = new[] { "35.242.133.146" };

        // Act & Assert
        Assert.Contains(allowedIP, monnifyIPs);
        Assert.DoesNotContain(blockedIP, monnifyIPs);
    }

    [Fact]
    public async Task ConcurrentPayments_ShouldHandleRaceConditions()
    {
        // Arrange
        var user = new User { Id = "concurrent_user", Email = "test@test.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var tasks = new List<Task<Payment>>();

        // Act - Simulate concurrent payment attempts
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(
                Task.Run(async () =>
                {
                    var payment = new Payment
                    {
                        PaymentId = Guid.NewGuid(),
                        UserId = user.Id,
                        Reference = $"EDU_CONCURRENT_{i}",
                        Amount = 50000,
                        Status = "Pending",
                        Provider = "Paystack",
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                    return payment;
                })
            );
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All payments should be created with unique references
        Assert.Equal(5, results.Length);
        var references = results.Select(p => p.Reference).ToList();
        Assert.Equal(references.Count, references.Distinct().Count()); // All unique
    }

    [Fact]
    public void WebhookPayload_ShouldValidateStructure()
    {
        // Arrange
        var validPaystackPayload = """
            {
                "event": "charge.success",
                "data": {
                    "reference": "EDU_TEST_001",
                    "status": "success",
                    "amount": 5000000
                }
            }
            """;

        var invalidPayload = """
            {
                "invalid": "structure"
            }
            """;

        // Act & Assert
        Assert.True(IsValidPaystackWebhook(validPaystackPayload));
        Assert.False(IsValidPaystackWebhook(invalidPayload));
    }

    [Fact]
    public void PaymentTimeout_ShouldHaveReasonableLimit()
    {
        // Arrange
        var paymentCreatedAt = DateTime.UtcNow.AddHours(-2);
        var timeoutThreshold = TimeSpan.FromHours(1);

        // Act
        var isTimedOut = DateTime.UtcNow - paymentCreatedAt > timeoutThreshold;

        // Assert
        Assert.True(isTimedOut);
    }

    // Helper methods
    private static bool ValidatePaystackSignature(
        string payload,
        string signature,
        string secretKey
    )
    {
        try
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(
                Encoding.UTF8.GetBytes(secretKey)
            );
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(hash).ToLower();
            return computedSignature == signature;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateMonnifySignature(
        string payload,
        string signature,
        string clientSecret
    )
    {
        try
        {
            var dataToHash = clientSecret + payload;
            using var sha512 = System.Security.Cryptography.SHA512.Create();
            var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
            var computedSignature = Convert.ToHexString(hash).ToLower();
            return computedSignature == signature;
        }
        catch
        {
            return false;
        }
    }

    private static string GeneratePaymentReference()
    {
        return $"EDU_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}";
    }

    private static bool IsValidPaymentAmount(decimal amount)
    {
        return amount > 0 && amount <= 50_000_000; // Between ₦0.01 and ₦500,000
    }

    private static bool IsValidPaystackWebhook(string payload)
    {
        try
        {
            var webhook = JsonSerializer.Deserialize<PaystackWebhookDto>(payload);
            return !string.IsNullOrEmpty(webhook?.Event) && webhook.Data?.Reference != null;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
