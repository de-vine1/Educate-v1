namespace Educate.Application.Interfaces;

public interface IReceiptService
{
    Task<string> GenerateReceiptAsync(int paymentId);
    Task<byte[]> GetReceiptPdfAsync(int receiptId);
    Task SendReceiptEmailAsync(int paymentId);
}
