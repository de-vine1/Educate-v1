using System.Security.Claims;
using Educate.Application.Interfaces;
using Educate.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceiptController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IReceiptService _receiptService;

    public ReceiptController(AppDbContext context, IReceiptService receiptService)
    {
        _context = context;
        _receiptService = receiptService;
    }

    [HttpGet("{receiptId}/download")]
    public async Task<IActionResult> DownloadReceipt(int receiptId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var receipt = await _context
            .Receipts.Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.ReceiptId == receiptId && r.Payment.UserId == userId);

        if (receipt == null)
            return NotFound();

        var pdfBytes = await _receiptService.GetReceiptPdfAsync(receiptId);

        return File(pdfBytes, "application/pdf", $"receipt_{receipt.ReceiptNumber}.pdf");
    }

    [HttpGet("my-receipts")]
    public async Task<IActionResult> GetMyReceipts()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var receipts = await _context
            .Receipts.Include(r => r.Payment)
            .Where(r => r.Payment.UserId == userId)
            .Select(r => new
            {
                r.ReceiptId,
                r.ReceiptNumber,
                r.GeneratedAt,
                PaymentAmount = r.Payment.Amount,
                PaymentProvider = r.Payment.Provider,
                PaymentReference = r.Payment.Reference,
            })
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync();

        return Ok(receipts);
    }
}
