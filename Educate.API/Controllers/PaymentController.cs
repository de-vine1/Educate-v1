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

        _logger.LogInformation(
            "Paystack webhook received, Signature: {Signature}, Body: {Body}",
            signature,
            body
        );

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
        // IP Whitelisting for Monnify (Monnify sandbox and production IPs)
        var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        var allowedIps = new[] { "35.242.133.146", "::ffff:35.242.133.146", "127.0.0.1", "::1" }; // Added localhost for testing

        if (!allowedIps.Contains(clientIp))
        {
            _logger.LogWarning("Unauthorized IP attempting Monnify webhook: {IP}", clientIp);
            return Unauthorized();
        }

        var signature = Request.Headers["monnify-signature"].FirstOrDefault();
        var body = await new StreamReader(Request.Body).ReadToEndAsync();

        _logger.LogInformation(
            "Monnify webhook received from IP: {IP}, Signature: {Signature}, Body: {Body}",
            clientIp,
            signature,
            body
        );

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

    [HttpGet("test-monnify")]
    [Authorize]
    public async Task<IActionResult> TestMonnify()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var testRequest = new PaymentInitializationRequest
        {
            CourseId = 1, // Test course ID
            LevelId = 1, // Test level ID
            PaymentProvider = "Monnify",
        };

        var result = await _paymentService.InitializePaymentAsync(userId, testRequest);
        return Ok(result);
    }

    [HttpGet("test-paystack")]
    [Authorize]
    public async Task<IActionResult> TestPaystack()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var testRequest = new PaymentInitializationRequest
        {
            CourseId = 1, // Test course ID
            LevelId = 1, // Test level ID
            PaymentProvider = "Paystack",
        };

        var result = await _paymentService.InitializePaymentAsync(userId, testRequest);
        return Ok(result);
    }

    [HttpGet("verify/{reference}")]
    public async Task<IActionResult> VerifyPayment(string reference)
    {
        var isVerified = await _paymentService.VerifyPaymentAsync(reference);
        return Ok(new { verified = isVerified, reference });
    }

    [HttpPost("test-paystack-webhook")]
    public async Task<IActionResult> TestPaystackWebhook()
    {
        var testPayload =
            "{\"event\":\"charge.success\",\"data\":{\"reference\":\"EDU_20250904143736_112b1ec0\",\"status\":\"success\",\"amount\":50000}}";
        var signature = "test-signature";

        var result = await _paymentService.ProcessPaystackWebhookAsync(signature, testPayload);
        return Ok(new { processed = result, payload = testPayload });
    }

    [HttpPost("test-monnify-webhook")]
    public async Task<IActionResult> TestMonnifyWebhook()
    {
        var testPayload =
            "{\"eventType\":\"SUCCESSFUL_TRANSACTION\",\"eventData\":{\"paymentReference\":\"EDU_20250904143736_112b1ec0\",\"paymentStatus\":\"PAID\",\"amountPaid\":500}}";
        var signature = "test-signature";

        var result = await _paymentService.ProcessMonnifyWebhookAsync(signature, testPayload);
        return Ok(new { processed = result, payload = testPayload });
    }
}
