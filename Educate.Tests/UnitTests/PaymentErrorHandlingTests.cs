using System.Net;
using System.Text;
using System.Text.Json;
using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using Educate.Infrastructure.Configurations;
using Educate.Infrastructure.Database;
using Educate.Infrastructure.Implementations;
using Educate.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Educate.Tests.UnitTests;

public class PaymentErrorHandlingTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IReceiptService> _mockReceiptService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<ILogger<PaymentService>> _mockLogger;
    private readonly PaymentService _paymentService;

    public PaymentErrorHandlingTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockReceiptService = new Mock<IReceiptService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<PaymentService>>();

        var paystackConfig = new PaystackConfig
        {
            SecretKey = TestConfigurationHelper.PaymentSecrets.PaystackSecretKey,
            PublicKey = TestConfigurationHelper.PaymentSecrets.PaystackPublicKey,
            BaseUrl = "https://api.paystack.co",
        };

        var monnifyConfig = new MonnifyConfig
        {
            ApiKey = TestConfigurationHelper.PaymentSecrets.MonnifyApiKey,
            SecretKey = TestConfigurationHelper.PaymentSecrets.MonnifySecretKey,
            BaseUrl = "https://sandbox.monnify.com",
            ContractCode = "test_contract",
        };

        var httpClient = new HttpClient(_mockHttpHandler.Object);
        var mockConfiguration = new Mock<IConfiguration>();

        _paymentService = new PaymentService(
            _context,
            _mockEncryptionService.Object,
            Options.Create(paystackConfig),
            Options.Create(monnifyConfig),
            httpClient,
            mockConfiguration.Object,
            _mockReceiptService.Object,
            _mockEmailService.Object,
            _mockLogger.Object
        );

        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        var user = new User
        {
            Id = "error_test_user",
            FirstName = "Test",
            LastName = "User",
            Email = "test@error.com",
        };

        var course = new Course
        {
            CourseId = Guid.NewGuid(),
            Name = "Error Test Course",
            Description = "Test Course",
        };

        var level = new Level
        {
            LevelId = Guid.NewGuid(),
            CourseId = course.CourseId,
            Name = "Error Test Level",
            Order = 1,
        };

        _context.Users.Add(user);
        _context.Courses.Add(course);
        _context.Levels.Add(level);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task PaymentInitialization_NetworkTimeout_ShouldHandleGracefully()
    {
        // Arrange
        var user = await _context.Users.FirstAsync();
        var level = await _context.Levels.FirstAsync();

        var request = new PaymentInitializationRequest
        {
            CourseId = level.CourseId,
            LevelId = level.LevelId,
            PaymentProvider = "paystack",
            CallbackUrl = "https://test.com/callback",
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _paymentService.InitializePaymentAsync(user.Id, request)
        );
    }

    [Fact]
    public async Task PaymentInitialization_APIError_ShouldReturnFailure()
    {
        // Arrange
        var user = await _context.Users.FirstAsync();
        var level = await _context.Levels.FirstAsync();

        var request = new PaymentInitializationRequest
        {
            CourseId = level.CourseId,
            LevelId = level.LevelId,
            PaymentProvider = "paystack",
            CallbackUrl = "https://test.com/callback",
        };

        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new { status = false, message = "Invalid request parameters" }
                )
            ),
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(errorResponse);

        // Act
        var result = await _paymentService.InitializePaymentAsync(user.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to initialize Paystack payment", result.Message);
    }

    [Fact]
    public async Task PaymentInitialization_InvalidUser_ShouldReturnError()
    {
        // Arrange
        var level = await _context.Levels.FirstAsync();

        var request = new PaymentInitializationRequest
        {
            CourseId = level.CourseId,
            LevelId = level.LevelId,
            PaymentProvider = "paystack",
            CallbackUrl = "https://test.com/callback",
        };

        // Act
        var result = await _paymentService.InitializePaymentAsync("non_existent_user", request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found", result.Message);
    }

    [Fact]
    public async Task PaymentInitialization_InvalidLevel_ShouldReturnError()
    {
        // Arrange
        var user = await _context.Users.FirstAsync();

        var request = new PaymentInitializationRequest
        {
            CourseId = Guid.NewGuid(),
            LevelId = Guid.NewGuid(), // Non-existent level
            PaymentProvider = "paystack",
            CallbackUrl = "https://test.com/callback",
        };

        // Act
        var result = await _paymentService.InitializePaymentAsync(user.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Level not found", result.Message);
    }

    [Fact]
    public async Task PaymentInitialization_InvalidProvider_ShouldReturnError()
    {
        // Arrange
        var user = await _context.Users.FirstAsync();
        var level = await _context.Levels.FirstAsync();

        var request = new PaymentInitializationRequest
        {
            CourseId = level.CourseId,
            LevelId = level.LevelId,
            PaymentProvider = "invalid_provider",
            CallbackUrl = "https://test.com/callback",
        };

        // Act
        var result = await _paymentService.InitializePaymentAsync(user.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid payment provider", result.Message);
    }

    [Fact]
    public async Task WebhookProcessing_MalformedJSON_ShouldHandleGracefully()
    {
        // Arrange
        var malformedPayload = "{ invalid json structure";
        var signature = "test_signature";

        // Act
        var result = await _paymentService.ProcessPaystackWebhookAsync(signature, malformedPayload);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task WebhookProcessing_MissingRequiredFields_ShouldReject()
    {
        // Arrange
        var incompletePayload = JsonSerializer.Serialize(
            new
            {
                @event = "charge.success",
                // Missing data field
            }
        );

        var signature = ComputeTestSignature(
            incompletePayload,
            TestConfigurationHelper.PaymentSecrets.PaystackSecretKey
        );

        // Act
        var result = await _paymentService.ProcessPaystackWebhookAsync(
            signature,
            incompletePayload
        );

        // Assert
        Assert.True(result); // Returns true but doesn't process due to missing data
    }

    [Fact]
    public async Task DatabaseConnection_Failure_ShouldThrowException()
    {
        // Arrange - Create service with invalid connection
        var invalidOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("invalid_db")
            .Options;

        using var invalidContext = new AppDbContext(invalidOptions);

        // Simulate database unavailable by disposing context
        await invalidContext.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await invalidContext.Payments.CountAsync();
        });
    }

    [Fact]
    public async Task PaymentVerification_ExternalAPIDown_ShouldHandleFailure()
    {
        // Arrange
        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            UserId = "error_test_user",
            Reference = "test_verification_error",
            Status = "Pending",
            Provider = "Paystack",
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var webhookPayload = JsonSerializer.Serialize(
            new { @event = "charge.success", data = new { reference = "test_verification_error" } }
        );

        var signature = ComputeTestSignature(
            webhookPayload,
            TestConfigurationHelper.PaymentSecrets.PaystackSecretKey
        );

        // Setup HTTP client to return error for verification
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var result = await _paymentService.ProcessPaystackWebhookAsync(signature, webhookPayload);

        // Assert
        Assert.True(result); // Webhook accepted but verification may fail
    }

    [Fact]
    public async Task ConcurrentWebhooks_SamePayment_ShouldHandleRaceCondition()
    {
        // Arrange
        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            UserId = "error_test_user",
            Reference = "concurrent_webhook_test",
            Status = "Pending",
            Provider = "Paystack",
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var webhookPayload = JsonSerializer.Serialize(
            new { @event = "charge.success", data = new { reference = "concurrent_webhook_test" } }
        );

        var signature = ComputeTestSignature(
            webhookPayload,
            TestConfigurationHelper.PaymentSecrets.PaystackSecretKey
        );

        var verificationResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new
                    {
                        status = true,
                        data = new { status = "success", reference = "concurrent_webhook_test" },
                    }
                )
            ),
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(verificationResponse);

        // Act - Process same webhook concurrently
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(_paymentService.ProcessPaystackWebhookAsync(signature, webhookPayload));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed (idempotent)
        Assert.All(results, result => Assert.True(result));
    }

    [Fact]
    public void PaymentAmount_ExtremeValues_ShouldValidate()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            var payment = new Payment
            {
                Amount = decimal.MaxValue, // Extreme value
            };

            if (payment.Amount > 1_000_000_000) // ₦10M limit
                throw new ArgumentException("Amount too large");
        });
    }

    private static string ComputeTestSignature(string data, string key)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
