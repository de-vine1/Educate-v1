using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using Educate.Infrastructure.Configurations;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    public PaymentService(
        AppDbContext context,
        IEncryptionService encryptionService,
        IOptions<PaystackConfig> paystackConfig,
        IOptions<MonnifyConfig> monnifyConfig,
        HttpClient httpClient,
        IConfiguration configuration,
        IReceiptService receiptService
    )
    {
        _context = context;
        _encryptionService = encryptionService;
        _paystackConfig = paystackConfig.Value;
        _monnifyConfig = monnifyConfig.Value;
        _httpClient = httpClient;
        _configuration = configuration;
        _receiptService = receiptService;
    }

    public async Task<string> ProcessPaymentAsync(string userId, Guid courseId, string cardToken)
    {
        var reference = Guid.NewGuid().ToString();

        var payment = new Payment
        {
            UserId = userId,
            Amount = 50000,
            Provider = "Paystack",
            Reference = reference,
            Status = "Pending",
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return reference;
    }

    public async Task<bool> VerifyPaymentAsync(string reference)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
        return payment?.Status == "Success";
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
        var amount = 50000;

        var payment = new Payment
        {
            UserId = userId,
            CourseId = request.CourseId,
            LevelId = request.LevelId,
            Amount = amount,
            Provider = request.PaymentProvider,
            Reference = reference,
            Status = "Pending",
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return request.PaymentProvider.ToLower() switch
        {
            "paystack" => await InitializePaystackPayment(
                user.Email!,
                amount,
                reference,
                request.CallbackUrl
            ),
            "monnify" => await InitializeMonnifyPayment(
                user.FirstName + " " + user.LastName,
                user.Email!,
                amount,
                reference,
                request.CallbackUrl
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
        var requestData = new PaystackInitializeRequest
        {
            Amount = (int)(amount * 100),
            Email = email,
            Reference = reference,
            Callback_url = callbackUrl,
        };

        var json = JsonSerializer.Serialize(requestData);
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

        if (response.IsSuccessStatusCode)
        {
            var paystackResponse = JsonSerializer.Deserialize<PaystackInitializeResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return new PaymentInitializationResponse
            {
                Success = paystackResponse?.Status == true,
                PaymentUrl = paystackResponse?.Data?.Authorization_url ?? string.Empty,
                Reference = reference,
                Message = paystackResponse?.Message ?? "Payment initialized successfully",
            };
        }

        return new PaymentInitializationResponse
        {
            Success = false,
            Message = "Failed to initialize Paystack payment",
        };
    }

    private async Task<PaymentInitializationResponse> InitializeMonnifyPayment(
        string customerName,
        string email,
        decimal amount,
        string reference,
        string callbackUrl
    )
    {
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
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var authString = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_monnifyConfig.ApiKey}:{_monnifyConfig.SecretKey}")
        );
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");

        var response = await _httpClient.PostAsync(
            $"{_monnifyConfig.BaseUrl}/api/v1/merchant/transactions/init-transaction",
            content
        );
        var responseContent = await response.Content.ReadAsStringAsync();

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
                Message = monnifyResponse?.ResponseMessage ?? "Payment initialized successfully",
            };
        }

        return new PaymentInitializationResponse
        {
            Success = false,
            Message = "Failed to initialize Monnify payment",
        };
    }

    public async Task<bool> ProcessPaystackWebhookAsync(string signature, string payload)
    {
        if (!VerifyPaystackSignature(payload, signature))
            return false;

        var webhook = JsonSerializer.Deserialize<PaystackWebhookDto>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (webhook?.Event == null || string.IsNullOrEmpty(webhook.Data?.Reference))
            return true;

        _ = Task.Run(async () =>
            await ProcessPaystackEventAsync(webhook.Event, webhook.Data.Reference)
        );
        return true;
    }

    public async Task<bool> ProcessMonnifyWebhookAsync(string signature, string payload)
    {
        if (!VerifyMonnifySignature(payload, signature))
            return false;

        var webhook = JsonSerializer.Deserialize<MonnifyWebhookDto>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (webhook?.EventType == null || string.IsNullOrEmpty(webhook.EventData?.PaymentReference))
            return true;

        _ = Task.Run(async () =>
            await ProcessMonnifyEventAsync(webhook.EventType, webhook.EventData.PaymentReference)
        );
        return true;
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

    private async Task<bool> VerifyAndUpdatePaymentAsync(string reference, string provider)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
        if (payment == null || payment.Status != "Pending")
            return true;

        var isVerified =
            provider == "Paystack"
                ? await VerifyPaystackTransactionAsync(reference)
                : await VerifyMonnifyTransactionAsync(reference);

        if (isVerified)
        {
            payment.Status = "Success";
            await CreateUserSubscriptionAsync(payment.UserId, payment.PaymentId);

            // Generate and send receipt
            _ = Task.Run(async () =>
            {
                try
                {
                    await _receiptService.GenerateReceiptAsync(payment.PaymentId);
                    await _receiptService.SendReceiptEmailAsync(payment.PaymentId);
                }
                catch (Exception)
                {
                    // Log error but don't fail the payment process
                }
            });
        }
        else
        {
            payment.Status = "Failed";
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
        var authString = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_monnifyConfig.ApiKey}:{_monnifyConfig.SecretKey}")
        );
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");

        var response = await _httpClient.GetAsync(
            $"{_monnifyConfig.BaseUrl}/api/v2/transactions/{reference}"
        );
        if (!response.IsSuccessStatusCode)
            return false;

        var content = await response.Content.ReadAsStringAsync();
        var verification = JsonSerializer.Deserialize<MonnifyVerificationResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return verification?.RequestSuccessful == true
            && verification.ResponseBody?.PaymentStatus == "PAID";
    }

    private async Task CreateUserSubscriptionAsync(string userId, Guid paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment?.CourseId == null || payment.LevelId == null)
            return;

        // Check if user already has an active subscription for this course/level
        var existingSubscription = await _context.UserCourses.FirstOrDefaultAsync(uc =>
            uc.UserId == userId
            && uc.CourseId == payment.CourseId.Value
            && uc.LevelId == payment.LevelId.Value
            && uc.Status == "Active"
        );

        if (existingSubscription != null)
        {
            // Extend existing subscription by 6 months
            existingSubscription.SubscriptionEndDate =
                existingSubscription.SubscriptionEndDate.AddMonths(6);
            existingSubscription.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new subscription
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
        }

        await _context.SaveChangesAsync();
    }

    private async Task ProcessPaystackEventAsync(string eventType, string reference)
    {
        try
        {
            switch (eventType)
            {
                case "charge.success":
                    await VerifyAndUpdatePaymentAsync(reference, "Paystack");
                    break;
                case "charge.failed":
                    await HandleFailedTransactionAsync(reference);
                    break;
            }
        }
        catch (Exception)
        {
            // Log error but don't throw - webhook already acknowledged
        }
    }

    private async Task ProcessMonnifyEventAsync(string eventType, string reference)
    {
        try
        {
            switch (eventType)
            {
                case "SUCCESSFUL_TRANSACTION":
                    await VerifyAndUpdatePaymentAsync(reference, "Monnify");
                    break;
                case "FAILED_TRANSACTION":
                    await HandleFailedTransactionAsync(reference);
                    break;
            }
        }
        catch (Exception)
        {
            // Log error but don't throw - webhook already acknowledged
        }
    }

    private async Task HandleFailedTransactionAsync(string reference)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
        if (payment != null)
        {
            payment.Status = "Failed";
            await _context.SaveChangesAsync();
        }
    }
}
