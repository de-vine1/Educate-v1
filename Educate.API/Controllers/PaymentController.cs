using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Educate.Application.Interfaces;
using Educate.Application.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPaymentService _paymentService;

    public PaymentController(
        ILogger<PaymentController> logger,
        IConfiguration configuration,
        IPaymentService paymentService
    )
    {
        _logger = logger;
        _configuration = configuration;
        _paymentService = paymentService;
    }

    [HttpPost("initialize")]
    [Authorize]
    public async Task<IActionResult> InitializePayment(
        [FromBody] PaymentInitializationRequest request
    )
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _paymentService.InitializePaymentAsync(userId, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("paystack/webhook")]
    public async Task<IActionResult> PaystackWebhook()
    {
        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();
        var body = await new StreamReader(Request.Body).ReadToEndAsync();

        var processed = await _paymentService.ProcessPaystackWebhookAsync(
            signature ?? string.Empty,
            body
        );

        if (!processed)
        {
            _logger.LogWarning("Invalid Paystack webhook signature");
            return Unauthorized();
        }

        _logger.LogInformation("Paystack webhook processed successfully");
        return Ok();
    }

    [HttpPost("monnify/webhook")]
    public async Task<IActionResult> MonnifyWebhook()
    {
        // IP Whitelisting for Monnify
        var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        if (clientIp != "35.242.133.146" && clientIp != "::ffff:35.242.133.146")
        {
            _logger.LogWarning("Unauthorized IP attempting Monnify webhook: {IP}", clientIp);
            return Unauthorized();
        }

        var signature = Request.Headers["monnify-signature"].FirstOrDefault();
        var body = await new StreamReader(Request.Body).ReadToEndAsync();

        var processed = await _paymentService.ProcessMonnifyWebhookAsync(
            signature ?? string.Empty,
            body
        );

        if (!processed)
        {
            _logger.LogWarning("Invalid Monnify webhook signature");
            return Unauthorized();
        }

        _logger.LogInformation("Monnify webhook processed successfully");
        return Ok();
    }
}
