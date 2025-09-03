namespace Educate.Application.Interfaces;

public interface IReceiptService
{
    Task<string> GenerateReceiptAsync(Guid paymentId);
    Task<byte[]> GetReceiptPdfAsync(Guid receiptId);
    Task SendReceiptEmailAsync(Guid paymentId);
}
