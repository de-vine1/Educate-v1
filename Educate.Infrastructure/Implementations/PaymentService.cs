using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Educate.Domain.Entities;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Educate.Infrastructure.Implementations;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IAppDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly string _secretKey;

    public PaymentService(
        HttpClient httpClient,
        IConfiguration configuration,
        IAppDbContext context,
        ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _context = context;
        _logger = logger;
        _secretKey = _configuration["Paystack:SecretKey"] ?? "";
        
        _httpClient.BaseAddress = new Uri("https://api.paystack.co");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<PaymentInitializationResponse> InitializePaymentAsync(string userId, Guid levelId, string email, decimal amount)
    {
        // Amount is in kobo
        var amountInKobo = (int)(amount * 100);
        
        // This callback URL should be your frontend or API endpoint that handles the verification
        // For API-only, we might redirect to a client page that calls verify
        var callbackUrl = _configuration["Paystack:CallbackUrl"] ?? "http://localhost:3000/payment/callback";

        var requestObj = new
        {
            email = email,
            amount = amountInKobo,
            currency = "NGN",
            callback_url = callbackUrl,
            metadata = new
            {
                userId = userId,
                levelId = levelId,
                custom_fields = new[]
                {
                    new { display_name = "Level ID", variable_name = "level_id", value = levelId.ToString() }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestObj), Encoding.UTF8, "application/json");

        try 
        {
            var response = await _httpClient.PostAsync("/transaction/initialize", content);
            var responseString = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paystack initialization failed: {Response}", responseString);
                return new PaymentInitializationResponse(false, "Failed to initialize payment", "", "", "");
            }

            var paystackResponse = JsonSerializer.Deserialize<PaystackInitResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paystackResponse?.Status == true && paystackResponse.Data != null)
            {
                // Create Pending Payment Record
                var payment = Payment.Create(
                    userId,
                    levelId,
                    amount,
                    "Paystack",
                    paystackResponse.Data.Reference,
                    null // No card token
                );

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(CancellationToken.None);

                return new PaymentInitializationResponse(
                    true, 
                    "Authorization URL created", 
                    paystackResponse.Data.AuthorizationUrl, 
                    paystackResponse.Data.AccessCode, 
                    paystackResponse.Data.Reference
                );
            }

            return new PaymentInitializationResponse(false, paystackResponse?.Message ?? "Unknown error", "", "", "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Paystack initialization");
            return new PaymentInitializationResponse(false, "System error occurred", "", "", "");
        }
    }

    public async Task<PaymentVerificationResponse> VerifyPaymentAsync(string reference)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/transaction/verify/{reference}");
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PaymentVerificationResponse(false, "Verification failed", "Gateway Error", reference);
            }

            var paystackResponse = JsonSerializer.Deserialize<PaystackVerifyResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paystackResponse?.Status == true && paystackResponse.Data?.Status == "success")
            {
                // Update local payment record
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == reference);
                if (payment != null && payment.Status != "Success")
                {
                    payment.MarkAsSuccess();
                    
                    // Activate subscription logic could be triggered here or via a separate command/event
                    // For now, checks are done in payment service, but ideally we publish 'PaymentSucceeded' event.
                    
                    // Let's create a subscription if it doesn't exist? 
                    // The requirement says "Verify transactions & activate subscriptions".
                    // I will add subscription creation here for simplicity, or better, leverage MediatR from Controller.
                    // Sticking to single responsibility: Service just verifies and updates Payment status.
                    // Subscription creation should happen in the Handler that calls this verify.
                    
                    await _context.SaveChangesAsync(CancellationToken.None);
                }

                return new PaymentVerificationResponse(true, "Payment successful", paystackResponse.Data.GatewayResponse, reference);
            }

            return new PaymentVerificationResponse(false, paystackResponse?.Message ?? "Verification failed", paystackResponse?.Data?.GatewayResponse ?? "Failed", reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Paystack verification");
            return new PaymentVerificationResponse(false, "System error", "Error", reference);
        }
    }

    public bool ValidateSignature(string body, string signature)
    {
        if (string.IsNullOrEmpty(body) || string.IsNullOrEmpty(signature))
            return false;

        var secretKeyBytes = Encoding.UTF8.GetBytes(_secretKey);
        var inputBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new System.Security.Cryptography.HMACSHA512(secretKeyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return hashString.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    // Internal DTOs for Paystack JSON mapping
    private class PaystackInitResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public PaystackInitData Data { get; set; }
    }

    private class PaystackInitData
    {
        [JsonPropertyName("authorization_url")]
        public string AuthorizationUrl { get; set; }
        [JsonPropertyName("access_code")]
        public string AccessCode { get; set; }
        [JsonPropertyName("reference")]
        public string Reference { get; set; }
    }

    private class PaystackVerifyResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public PaystackVerifyData Data { get; set; }
    }

    private class PaystackVerifyData
    {
        public string Status { get; set; } // "success", "failed", "abandoned"
        public string Reference { get; set; }
        public decimal Amount { get; set; }
        [JsonPropertyName("gateway_response")]
        public string GatewayResponse { get; set; }
    }
}
