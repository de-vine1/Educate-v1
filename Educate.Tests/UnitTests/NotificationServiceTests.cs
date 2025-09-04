using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Educate.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Educate.Tests.UnitTests;

public class NotificationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        _notificationService = new NotificationService(
            _context,
            _mockEmailService.Object,
            _mockLogger.Object
        );

        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = "user123",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
        };

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task SendPaymentSuccessNotificationAsync_ShouldCreateNotificationAndSendEmail()
    {
        // Arrange
        var userId = "user123";
        var courseName = "ATS Examination";
        var levelName = "ATS1";
        var paymentReference = "EDU_20240101_TEST123";
        var amount = 50000m;

        // Act
        await _notificationService.SendPaymentSuccessNotificationAsync(
            userId,
            courseName,
            levelName,
            paymentReference,
            amount
        );

        // Assert
        var notification = await _context.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Equal("PAYMENT_SUCCESS", notification.Type);
        Assert.Equal("Payment Successful!", notification.Title);
        Assert.Contains(courseName, notification.Message);
        Assert.Contains(levelName, notification.Message);

        _mockEmailService.Verify(
            x =>
                x.SendEmailAsync("john.doe@example.com", "Payment Successful!", It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SendPaymentFailedNotificationAsync_ShouldCreateNotificationWithRetryInstructions()
    {
        // Arrange
        var userId = "user123";
        var courseName = "ICAN Examination";
        var levelName = "Foundation";
        var paymentReference = "EDU_20240101_FAIL123";

        // Act
        await _notificationService.SendPaymentFailedNotificationAsync(
            userId,
            courseName,
            levelName,
            paymentReference
        );

        // Assert
        var notification = await _context.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Equal("PAYMENT_FAILED", notification.Type);
        Assert.Equal("Payment Failed", notification.Title);
        Assert.Contains("unsuccessful", notification.Message);
        Assert.Contains(paymentReference, notification.Message);

        _mockEmailService.Verify(
            x =>
                x.SendEmailAsync(
                    It.IsAny<string>(),
                    "Payment Failed",
                    It.Is<string>(body => body.Contains("retry"))
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(1, "Subscription Expires Today!")]
    [InlineData(7, "Subscription Expiring in 7 Days")]
    [InlineData(14, "Subscription Expiring in 14 Days")]
    public async Task SendExpiryReminderAsync_ShouldCreateUrgentNotificationBasedOnDaysRemaining(
        int daysRemaining,
        string expectedTitle
    )
    {
        // Arrange
        var userId = "user123";
        var courseName = "ATS Examination";
        var levelName = "ATS2";
        var expiryDate = DateTime.UtcNow.AddDays(daysRemaining);

        // Act
        await _notificationService.SendExpiryReminderAsync(
            userId,
            courseName,
            levelName,
            expiryDate,
            daysRemaining
        );

        // Assert
        var notification = await _context.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Equal("SUBSCRIPTION_EXPIRY", notification.Type);
        Assert.Equal(expectedTitle, notification.Title);
        Assert.Contains("expires", notification.Message);

        _mockEmailService.Verify(
            x =>
                x.SendEmailAsync(
                    It.IsAny<string>(),
                    expectedTitle,
                    It.Is<string>(body => body.Contains("Renew"))
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldReturnUserNotificationsOrderedByDate()
    {
        // Arrange
        var userId = "user123";

        await _notificationService.CreateInAppNotificationAsync(
            userId,
            "PAYMENT_SUCCESS",
            "Payment 1",
            "First payment"
        );

        await Task.Delay(100); // Ensure different timestamps

        await _notificationService.CreateInAppNotificationAsync(
            userId,
            "PAYMENT_SUCCESS",
            "Payment 2",
            "Second payment"
        );

        // Act
        var notifications = await _notificationService.GetUserNotificationsAsync(userId);

        // Assert
        Assert.Equal(2, notifications.Count);
        var firstNotification = notifications.First() as dynamic;
        Assert.Equal("Payment 2", firstNotification.Title); // Most recent first
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithUnreadOnlyFilter_ShouldReturnOnlyUnread()
    {
        // Arrange
        var userId = "user123";

        await _notificationService.CreateInAppNotificationAsync(
            userId,
            "PAYMENT_SUCCESS",
            "Unread",
            "Unread message"
        );

        var notification = await _context.Notifications.FirstAsync();
        notification.IsRead = true; // Mark as read
        await _context.SaveChangesAsync();

        await _notificationService.CreateInAppNotificationAsync(
            userId,
            "PAYMENT_FAILED",
            "Unread 2",
            "Another unread message"
        );

        // Act
        var unreadNotifications = await _notificationService.GetUserNotificationsAsync(
            userId,
            unreadOnly: true
        );

        // Assert
        Assert.Single(unreadNotifications);
        var unreadNotification = unreadNotifications.First() as dynamic;
        Assert.Equal("Unread 2", unreadNotification.Title);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldUpdateNotificationReadStatus()
    {
        // Arrange
        var userId = "user123";
        await _notificationService.CreateInAppNotificationAsync(
            userId,
            "TEST",
            "Test Notification",
            "Test message"
        );

        var notification = await _context.Notifications.FirstAsync();
        Assert.False(notification.IsRead);

        // Act
        await _notificationService.MarkAsReadAsync(notification.NotificationId);

        // Assert
        var updatedNotification = await _context.Notifications.FindAsync(
            notification.NotificationId
        );
        Assert.True(updatedNotification.IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllUserNotificationsAsRead()
    {
        // Arrange
        var userId = "user123";

        await _notificationService.CreateInAppNotificationAsync(
            userId,
            "TEST1",
            "Test 1",
            "Message 1"
        );
        await _notificationService.CreateInAppNotificationAsync(
            userId,
            "TEST2",
            "Test 2",
            "Message 2"
        );

        var allNotifications = await _context.Notifications.ToListAsync();
        Assert.All(allNotifications, n => Assert.False(n.IsRead));

        // Act
        await _notificationService.MarkAllAsReadAsync(userId);

        // Assert
        var updatedNotifications = await _context.Notifications.ToListAsync();
        Assert.All(updatedNotifications, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task EmailFailure_ShouldLogErrorButNotThrow()
    {
        // Arrange
        var userId = "user123";
        _mockEmailService
            .Setup(x =>
                x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            )
            .ThrowsAsync(new Exception("Email service unavailable"));

        // Act & Assert - Should not throw
        await _notificationService.SendPaymentSuccessNotificationAsync(
            userId,
            "Test Course",
            "Test Level",
            "REF123",
            1000m
        );

        // Verify error was logged
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString().Contains("Failed to send payment success email")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
            Times.Once
        );

        // But notification should still be created
        var notification = await _context.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
