namespace Educate.Application.Interfaces;

public interface IPaymentService
{
    Task<string> ProcessPaymentAsync(string userId, int courseId, string cardToken);
    Task<bool> VerifyPaymentAsync(string transactionId);
}
