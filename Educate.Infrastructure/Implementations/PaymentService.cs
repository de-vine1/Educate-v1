using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using Educate.Domain.Enums;
using Educate.Infrastructure.Configurations;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Educate.Infrastructure.Implementations;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly PaystackConfig _paystackConfig;
    private readonly MonnifyConfig _monnifyConfig;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IReceiptService _receiptService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentService> _logger;

    private static readonly JsonSerializerOptions MonnifyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public PaymentService(
        AppDbContext context,
        IEncryptionService encryptionService,
        IOptions<PaystackConfig> paystackConfig,
        IOptions<MonnifyConfig> monnifyConfig,
        HttpClient httpClient,
        IConfiguration configuration,
        IReceiptService receiptService,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<PaymentService> logger
    )
    {
        _context = context;
        _encryptionService = encryptionService;
        _paystackConfig = paystackConfig.Value;
        _monnifyConfig = monnifyConfig.Value;
        _httpClient = httpClient;
        _configuration = configuration;
        _receiptService = receiptService;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<string> ProcessPaymentAsync(string userId, int courseId, string cardToken)
    {
        var reference = Guid.NewGuid().ToString();

        var payment = new Payment
        {
            UserId = userId,
            Amount = 5000,
            Provider = PaymentProvider.Paystack,
            Reference = reference,
            Status = PaymentStatus.Pending,
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return reference;
    }

    public async Task<bool> VerifyPaymentAsync(string reference)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
        return payment?.Status == PaymentStatus.Success;
    }

    public async Task<PaymentInitializationResponse> InitializePaymentAsync(
        string userId,
        PaymentInitializationRequest request
    )
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new PaymentInitializationResponse
            {
                Success = false,
                Message = "User not found",
            };

        var level = await _context.Levels.FindAsync(request.LevelId);
        if (level == null)
            return new PaymentInitializationResponse
            {
                Success = false,
                Message = "Level not found",
            };

        var reference = $"EDU_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}";
        var amount = 5000;

        var payment = new Payment
        {
            UserId = userId,
            CourseId = request.CourseId,
            LevelId = request.LevelId,
            Amount = amount,
            Provider = Enum.Parse<PaymentProvider>(request.PaymentProvider, true),
            Reference = reference,
            Status = PaymentStatus.Pending,
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return request.PaymentProvider.ToLower() switch
        {
            "paystack" => await InitializePaystackPayment(
                user.Email!,
                amount,
                reference,
                "https://educate.com/payment/callback"
            ),
            "monnify" => await InitializeMonnifyPayment(
                user.FirstName + " " + user.LastName,
                user.Email!,
                amount,
                reference,
                "https://educate.com/payment/callback"
            ),
            _ => new PaymentInitializationResponse
            {
                Success = false,
                Message = "Invalid payment provider",
            },
        };
    }

    private async Task<PaymentInitializationResponse> InitializePaystackPayment(
        string email,
        decimal amount,
        string reference,
        string callbackUrl
    )
    {
        try
        {
            var requestData = new PaystackInitializeRequest
            {
                Amount = amount.ToString("F0"), // Convert to string as per Paystack docs
                Email = email,
                Reference = reference,
                Callback_url = callbackUrl,
                Currency = "NGN",
                Channels = new[] { "card", "bank", "ussd", "qr", "mobile_money", "bank_transfer" },
            };

            var json = JsonSerializer.Serialize(requestData);

            _logger.LogInformation(
                "Paystack request: {Json}, SecretKey: {SecretKey}",
                json,
                _paystackConfig.SecretKey?.Substring(0, 10) + "..."
            );

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {_paystackConfig.SecretKey}"
            );

            var response = await _httpClient.PostAsync(
                $"{_paystackConfig.BaseUrl}/transaction/initialize",
                content
            );
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "Paystack response: {StatusCode} - {Response}",
                response.StatusCode,
                responseContent
            );

            if (response.IsSuccessStatusCode)
            {
                var paystackResponse = JsonSerializer.Deserialize<PaystackInitializeResponse>(
                    responseContent,
                    DeserializeOptions
                );

                return new PaymentInitializationResponse
                {
                    Success = paystackResponse?.Status == true,
                    PaymentUrl = paystackResponse?.Data?.Authorization_url ?? string.Empty,
                    Reference = paystackResponse?.Data?.Reference ?? reference,
                    Message = paystackResponse?.Message ?? "Payment initialized successfully",
                };
            }

            _logger.LogError(
                "Paystack API error: {StatusCode} - {Response}",
                response.StatusCode,
                responseContent
            );
            return new PaymentInitializationResponse
            {
                Success = false,
                Message = $"Failed to initialize Paystack payment: {response.StatusCode}",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in InitializePaystackPayment");
            return new PaymentInitializationResponse
            {
                Success = false,
                Message = "Failed to initialize Paystack payment",
            };
        }
    }

    private async Task<PaymentInitializationResponse> InitializeMonnifyPayment(
        string customerName,
        string email,
        decimal amount,
        string reference,
        string callbackUrl
    )
    {
        try
        {
            // First get access token
            var accessToken = await GetMonnifyAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new PaymentInitializationResponse
                {
                    Success = false,
                    Message = "Failed to get Monnify access token",
                };
            }

            var requestData = new MonnifyInitializeRequest
            {
                Amount = amount,
                CustomerName = customerName,
                CustomerEmail = email,
                PaymentReference = reference,
                PaymentDescription = "Educate Platform Subscription",
                CurrencyCode = "NGN",
                ContractCode = _monnifyConfig.ContractCode,
                RedirectUrl = callbackUrl,
                PaymentMethods = new[] { "CARD", "ACCOUNT_TRANSFER", "USSD", "PHONE_NUMBER" },
            };

            var json = JsonSerializer.Serialize(requestData, MonnifyJsonOptions);

            _logger.LogInformation("Monnify request: {Json}", json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.PostAsync(
                $"{_monnifyConfig.BaseUrl}/api/v1/merchant/transactions/init-transaction",
                content
            );
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "Monnify response: {StatusCode} - {Response}",
                response.StatusCode,
                responseContent
            );

            if (response.IsSuccessStatusCode)
            {
                var monnifyResponse = JsonSerializer.Deserialize<MonnifyInitializeResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return new PaymentInitializationResponse
                {
                    Success = monnifyResponse?.RequestSuccessful == true,
                    PaymentUrl = monnifyResponse?.ResponseBody?.CheckoutUrl ?? string.Empty,
                    Reference = reference,
                    Message =
                        monnifyResponse?.ResponseMessage ?? "Payment initialized successfully",
                };
            }

            _logger.LogError(
                "Monnify API error: {StatusCode} - {Response}",
                response.StatusCode,
                responseContent
            );
            return new PaymentInitializationResponse
            {
                Success = false,
                Message = $"Failed to initialize Monnify payment: {response.StatusCode}",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in InitializeMonnifyPayment");
            return new PaymentInitializationResponse
            {
                Success = false,
                Message = "Failed to initialize Monnify payment",
            };
        }
    }

    public Task<bool> ProcessPaystackWebhookAsync(string signature, string payload)
    {
        if (!VerifyPaystackSignature(payload, signature))
        {
            _logger.LogWarning(
                "Invalid Paystack webhook signature received. Payload: {Payload}",
                payload
            );
            return Task.FromResult(false);
        }

        var webhook = JsonSerializer.Deserialize<PaystackWebhookDto>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (webhook?.Event == null || string.IsNullOrEmpty(webhook.Data?.Reference))
            return Task.FromResult(true);

        _ = Task.Run(async () =>
            await ProcessPaystackEventAsync(webhook.Event, webhook.Data.Reference)
        );
        return Task.FromResult(true);
    }

    public Task<bool> ProcessMonnifyWebhookAsync(string signature, string payload)
    {
        if (!VerifyMonnifySignature(payload, signature))
        {
            _logger.LogWarning(
                "Invalid Monnify webhook signature received. Payload: {Payload}",
                payload
            );
            return Task.FromResult(false);
        }

        var webhook = JsonSerializer.Deserialize<MonnifyWebhookDto>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (webhook?.EventType == null || string.IsNullOrEmpty(webhook.EventData?.PaymentReference))
            return Task.FromResult(true);

        _ = Task.Run(async () =>
            await ProcessMonnifyEventAsync(webhook.EventType, webhook.EventData.PaymentReference)
        );
        return Task.FromResult(true);
    }

    private bool VerifyPaystackSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_paystackConfig.SecretKey) || string.IsNullOrEmpty(signature))
            return false;

        var hash = ComputeHmacSha512(payload, _paystackConfig.SecretKey);
        return hash.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    private bool VerifyMonnifySignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_monnifyConfig.SecretKey) || string.IsNullOrEmpty(signature))
            return false;

        var hash = ComputeMonnifyHash(payload, _monnifyConfig.SecretKey);
        return hash.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeHmacSha512(string data, string key)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    private static string ComputeMonnifyHash(string payload, string clientSecret)
    {
        var dataToHash = clientSecret + payload;
        using var sha512 = SHA512.Create();
        var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        return Convert.ToHexString(hash).ToLower();
    }

    private async Task<string> GetMonnifyAccessTokenAsync()
    {
        try
        {
            var authString = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_monnifyConfig.ApiKey}:{_monnifyConfig.SecretKey}")
            );

            _logger.LogInformation(
                "Monnify auth - ApiKey: {ApiKey}, BaseUrl: {BaseUrl}",
                _monnifyConfig.ApiKey?.Substring(0, 10) + "...",
                _monnifyConfig.BaseUrl
            );

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");

            var response = await _httpClient.PostAsync(
                $"{_monnifyConfig.BaseUrl}/api/v1/auth/login",
                new StringContent("{}", Encoding.UTF8, "application/json")
            );

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(
                "Monnify auth response: {StatusCode} - {Response}",
                response.StatusCode,
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to get Monnify access token: {StatusCode} - {Response}",
                    response.StatusCode,
                    content
                );
                return string.Empty;
            }

            var tokenResponse = JsonSerializer.Deserialize<MonnifyTokenResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return tokenResponse?.ResponseBody?.AccessToken ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting Monnify access token");
            return string.Empty;
        }
    }

    private async Task<bool> VerifyAndUpdatePaymentAsync(string reference, string provider)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found for reference: {Reference}", reference);
            return true;
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            _logger.LogInformation(
                "Duplicate webhook received for payment {Reference} with status {Status}",
                reference,
                payment.Status
            );
            return true; // Phase 4.6: Idempotency - already processed
        }

        // Phase 4.6: Block renewals if transaction is still pending
        if (payment.CourseId.HasValue && payment.LevelId.HasValue)
        {
            var pendingRenewal = await _context.Payments.AnyAsync(p =>
                p.UserId == payment.UserId
                && p.CourseId == payment.CourseId
                && p.LevelId == payment.LevelId
                && p.Status == PaymentStatus.Pending
                && p.PaymentId != payment.PaymentId
            );

            if (pendingRenewal)
            {
                _logger.LogWarning(
                    "Blocked renewal attempt - pending transaction exists for user {UserId}",
                    payment.UserId
                );
                return true;
            }
        }

        var isVerified =
            provider == "Paystack"
                ? await VerifyPaystackTransactionAsync(reference)
                : await VerifyMonnifyTransactionAsync(reference);

        if (isVerified)
        {
            payment.Status = PaymentStatus.Success;
            _logger.LogInformation(
                "Payment {Reference} verified successfully for user {UserId}",
                reference,
                payment.UserId
            );

            var isRenewal = await IsRenewalPaymentAsync(
                payment.UserId,
                payment.CourseId,
                payment.LevelId
            );
            await CreateUserSubscriptionAsync(payment.UserId, payment.PaymentId);

            // Send appropriate notification
            _ = Task.Run(async () =>
            {
                try
                {
                    var course = await _context.Courses.FindAsync(payment.CourseId);
                    var level = await _context.Levels.FindAsync(payment.LevelId);

                    if (course != null && level != null)
                    {
                        if (isRenewal)
                        {
                            var subscription = await _context.UserCourses.FirstOrDefaultAsync(uc =>
                                uc.UserId == payment.UserId
                                && uc.CourseId == payment.CourseId
                                && uc.LevelId == payment.LevelId
                            );
                            if (subscription != null)
                            {
                                await _notificationService.SendRenewalSuccessNotificationAsync(
                                    payment.UserId,
                                    course.Name,
                                    level.Name,
                                    subscription.SubscriptionEndDate
                                );
                            }
                        }
                        else
                        {
                            await _notificationService.SendPaymentSuccessNotificationAsync(
                                payment.UserId,
                                course.Name,
                                level.Name,
                                payment.Reference,
                                payment.Amount
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send payment notification for {PaymentId}",
                        payment.PaymentId
                    );
                }
            });

            // Generate and send receipt
            _ = Task.Run(async () =>
            {
                try
                {
                    await _receiptService.GenerateReceiptAsync(payment.PaymentId);
                    await _receiptService.SendReceiptEmailAsync(payment.PaymentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to generate/send receipt for payment {PaymentId}",
                        payment.PaymentId
                    );
                }
            });
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            _logger.LogWarning("Payment verification failed for reference: {Reference}", reference);

            // Send payment failed email
            _ = Task.Run(async () =>
            {
                try
                {
                    var user = await _context.Users.FindAsync(payment.UserId);
                    if (user != null)
                    {
                        await SendPaymentFailedEmailAsync(
                            user.Email!,
                            user.FirstName,
                            payment.Reference
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send payment failed email for reference {Reference}",
                        reference
                    );
                }
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> VerifyPaystackTransactionAsync(string reference)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {_paystackConfig.SecretKey}"
        );

        var response = await _httpClient.GetAsync(
            $"{_paystackConfig.BaseUrl}/transaction/verify/{reference}"
        );
        if (!response.IsSuccessStatusCode)
            return false;

        var content = await response.Content.ReadAsStringAsync();
        var verification = JsonSerializer.Deserialize<PaystackVerificationResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return verification?.Status == true && verification.Data?.Status == "success";
    }

    private async Task<bool> VerifyMonnifyTransactionAsync(string reference)
    {
        try
        {
            var accessToken = await GetMonnifyAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                return false;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync(
                $"{_monnifyConfig.BaseUrl}/api/v2/merchant/transactions/query?paymentReference={reference}"
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Monnify verification failed: {StatusCode}",
                    response.StatusCode
                );
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            var verification = JsonSerializer.Deserialize<MonnifyVerificationResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return verification?.RequestSuccessful == true
                && verification.ResponseBody?.PaymentStatus == "PAID";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception in VerifyMonnifyTransactionAsync for reference {Reference}",
                reference
            );
            return false;
        }
    }

    private async Task CreateUserSubscriptionAsync(string userId, int paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment?.CourseId == null || payment.LevelId == null)
            return;

        // Check if user has any subscription for this course/level (active, expired, or expiring)
        var existingSubscription = await _context.UserCourses.FirstOrDefaultAsync(uc =>
            uc.UserId == userId
            && uc.CourseId == payment.CourseId.Value
            && uc.LevelId == payment.LevelId.Value
        );

        if (existingSubscription != null)
        {
            // Phase 4.5: Renewal payment handling
            if (existingSubscription.Status == "Expired")
            {
                // If expired, set new StartDate = Now, EndDate = Now + 6 months
                existingSubscription.SubscriptionStartDate = DateTime.UtcNow;
                existingSubscription.SubscriptionEndDate = DateTime.UtcNow.AddMonths(6);
            }
            else
            {
                // Extend EndDate by 6 months from current expiry
                existingSubscription.SubscriptionEndDate =
                    existingSubscription.SubscriptionEndDate.AddMonths(6);
            }

            existingSubscription.Status = "Renewed";
            existingSubscription.RenewalCount += 1;
            existingSubscription.PaymentId = paymentId;
            existingSubscription.UpdatedAt = DateTime.UtcNow;

            // Phase 4.6: Log all renewal attempts for auditing
            var previousEndDate = existingSubscription.SubscriptionEndDate;
            var newEndDate =
                existingSubscription.Status == "Expired"
                    ? DateTime.UtcNow.AddMonths(6)
                    : existingSubscription.SubscriptionEndDate.AddMonths(6);

            var history = new Domain.Entities.SubscriptionHistory
            {
                SubscriptionId = existingSubscription.UserCourseId,
                UserId = userId,
                CourseId = payment.CourseId.Value,
                LevelId = payment.LevelId.Value,
                Action = "Renewed",
                PaymentReference = payment.Reference,
                Amount = payment.Amount,
                PaymentProvider = payment.Provider.ToString(),
                PreviousEndDate = previousEndDate,
                NewEndDate = newEndDate,
            };
            _context.SubscriptionHistories.Add(history);

            _logger.LogInformation(
                "Renewed subscription for user {UserId}, course {CourseId}, level {LevelId}",
                userId,
                payment.CourseId.Value,
                payment.LevelId.Value
            );
        }
        else
        {
            // Create new subscription with 6-month validity
            var userCourse = new UserCourse
            {
                UserId = userId,
                CourseId = payment.CourseId.Value,
                LevelId = payment.LevelId.Value,
                SubscriptionStartDate = DateTime.UtcNow,
                SubscriptionEndDate = DateTime.UtcNow.AddMonths(6),
                Status = "Active",
                PaymentId = paymentId,
            };

            _context.UserCourses.Add(userCourse);

            // Log creation in history
            var history = new Domain.Entities.SubscriptionHistory
            {
                SubscriptionId = userCourse.UserCourseId,
                UserId = userId,
                CourseId = payment.CourseId.Value,
                LevelId = payment.LevelId.Value,
                Action = "Created",
                PaymentReference = payment.Reference,
                Amount = payment.Amount,
                PaymentProvider = payment.Provider.ToString(),
                PreviousEndDate = DateTime.UtcNow,
                NewEndDate = DateTime.UtcNow.AddMonths(6),
            };
            _context.SubscriptionHistories.Add(history);

            _logger.LogInformation(
                "Created new subscription for user {UserId}, course {CourseId}, level {LevelId}",
                userId,
                payment.CourseId.Value,
                payment.LevelId.Value
            );
        }

        await _context.SaveChangesAsync();
    }

    private Task<bool> IsRenewalPaymentAsync(string userId, int? courseId, int? levelId)
    {
        if (!courseId.HasValue || !levelId.HasValue)
            return Task.FromResult(false);

        return _context.UserCourses.AnyAsync(uc =>
            uc.UserId == userId && uc.CourseId == courseId.Value && uc.LevelId == levelId.Value
        );
    }

    private async Task ProcessPaystackEventAsync(string eventType, string reference)
    {
        try
        {
            _logger.LogInformation(
                "Processing Paystack event {EventType} for reference {Reference}",
                eventType,
                reference
            );
            switch (eventType)
            {
                case "charge.success":
                    await VerifyAndUpdatePaymentAsync(reference, "Paystack");
                    break;
                case "charge.failed":
                    await HandleFailedTransactionAsync(reference);
                    break;
                default:
                    _logger.LogInformation("Unhandled Paystack event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing Paystack event {EventType} for reference {Reference}",
                eventType,
                reference
            );
        }
    }

    private async Task ProcessMonnifyEventAsync(string eventType, string reference)
    {
        try
        {
            _logger.LogInformation(
                "Processing Monnify event {EventType} for reference {Reference}",
                eventType,
                reference
            );
            switch (eventType)
            {
                case "SUCCESSFUL_TRANSACTION":
                case "OVERPAYMENT_TRANSACTION":
                    await VerifyAndUpdatePaymentAsync(reference, "Monnify");
                    break;
                case "FAILED_TRANSACTION":
                case "EXPIRED_TRANSACTION":
                    await HandleFailedTransactionAsync(reference);
                    break;
                default:
                    _logger.LogInformation("Unhandled Monnify event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing Monnify event {EventType} for reference {Reference}",
                eventType,
                reference
            );
        }
    }

    private async Task HandleFailedTransactionAsync(string reference)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
        if (payment != null)
        {
            payment.Status = PaymentStatus.Failed;
            await _context.SaveChangesAsync();
        }
    }

    private Task SendPaymentFailedEmailAsync(string email, string firstName, string reference)
    {
        return _emailService.SendEmailAsync(
            email,
            "Payment Failed - Educate Platform",
            $"Dear {firstName},\n\nYour payment with reference {reference} could not be processed.\n\nPlease try again or contact support if the issue persists.\n\nRetry Payment: https://educate.com/payment/retry\nSupport: support@educate.com\n\nBest regards,\nEducate Platform Team"
        );
    }
}
