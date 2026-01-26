using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;

namespace Educate.Application.Interfaces;



public interface IPaymentService
{
    Task<PaymentInitializationResponse> InitializePaymentAsync(string userId, Guid levelId, string email, decimal amount);
    Task<PaymentVerificationResponse> VerifyPaymentAsync(string reference);
    bool ValidateSignature(string body, string signature);
}
