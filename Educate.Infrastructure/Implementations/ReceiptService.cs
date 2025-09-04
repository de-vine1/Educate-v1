using Educate.Application.Interfaces;
using Educate.Domain.Entities;
using Educate.Domain.Enums;
using Educate.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Educate.Infrastructure.Implementations;

public class ReceiptService : IReceiptService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public ReceiptService(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GenerateReceiptAsync(int paymentId)
    {
        var payment = await _context
            .Payments.Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null || payment.Status != PaymentStatus.Success)
            throw new InvalidOperationException("Payment not found or not successful");

        var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{payment.Reference[^8..]}";
        var fileName = $"receipt_{receiptNumber}.pdf";
        var filePath = Path.Combine("receipts", fileName);
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var pdfBytes = GenerateReceiptPdf(payment, receiptNumber);
        await File.WriteAllBytesAsync(fullPath, pdfBytes);

        var receipt = new Receipt
        {
            PaymentId = paymentId,
            ReceiptNumber = receiptNumber,
            FilePath = filePath,
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();

        return receipt.ReceiptId.ToString();
    }

    public async Task<byte[]> GetReceiptPdfAsync(int receiptId)
    {
        var receipt = await _context.Receipts.FindAsync(receiptId);
        if (receipt == null)
            throw new FileNotFoundException("Receipt not found");

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", receipt.FilePath);
        return await File.ReadAllBytesAsync(fullPath);
    }

    public async Task SendReceiptEmailAsync(int paymentId)
    {
        var payment = await _context
            .Payments.Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
            throw new InvalidOperationException("Payment not found");

        var receipt = await _context.Receipts.FirstOrDefaultAsync(r => r.PaymentId == paymentId);

        if (receipt == null)
            throw new InvalidOperationException("Receipt not found");

        var pdfBytes = await GetReceiptPdfAsync(receipt.ReceiptId);

        await _emailService.SendEmailWithAttachmentAsync(
            payment.User.Email!,
            "Payment Receipt - Educate Platform",
            $"Dear {payment.User.FirstName},\n\nThank you for your payment. Please find your receipt attached.\n\nBest regards,\nEducate Platform Team",
            pdfBytes,
            $"receipt_{receipt.ReceiptNumber}.pdf"
        );
    }

    private byte[] GenerateReceiptPdf(Payment payment, string receiptNumber)
    {
        return Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header()
                        .Text("PAYMENT RECEIPT")
                        .SemiBold()
                        .FontSize(20)
                        .FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            column
                                .Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("Receipt Number:").SemiBold();
                                            col.Item().Text(receiptNumber);
                                        });

                                    row.RelativeItem()
                                        .Column(col =>
                                        {
                                            col.Item().Text("Date:").SemiBold();
                                            col.Item()
                                                .Text(payment.CreatedAt.ToString("dd/MM/yyyy"));
                                        });
                                });

                            column.Item().LineHorizontal(1);

                            column.Item().Text("Customer Details").SemiBold().FontSize(14);
                            column
                                .Item()
                                .Text($"Name: {payment.User.FirstName} {payment.User.LastName}");
                            column.Item().Text($"Email: {payment.User.Email}");

                            column.Item().LineHorizontal(1);

                            column.Item().Text("Payment Details").SemiBold().FontSize(14);
                            column.Item().Text($"Transaction Reference: {payment.Reference}");
                            column.Item().Text($"Payment Provider: {payment.Provider}");
                            column.Item().Text($"Amount: ₦{payment.Amount:N2}");
                            column.Item().Text($"Status: {payment.Status}");

                            column.Item().LineHorizontal(1);

                            column
                                .Item()
                                .Text("Thank you for your payment!")
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken2);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("Educate Platform - www.educate.com")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Medium);
                });
            })
            .GeneratePdf();
    }
}
