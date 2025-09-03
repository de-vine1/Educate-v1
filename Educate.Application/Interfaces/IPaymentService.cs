using Educate.Application.Models.DTOs;

namespace Educate.Application.Interfaces;

public interface IPaymentService
{
    Task<string> ProcessPaymentAsync(string userId, Guid courseId, string cardToken);
    Task<bool> VerifyPaymentAsync(string reference);
    Task<PaymentInitializationResponse> InitializePaymentAsync(
        string userId,
        PaymentInitializationRequest request
    );
    Task<bool> ProcessPaystackWebhookAsync(string signature, string payload);
    Task<bool> ProcessMonnifyWebhookAsync(string signature, string payload);
}
