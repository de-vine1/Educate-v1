using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _configuration;

    public PaymentController(ILogger<PaymentController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("paystack/webhook")]
    public async Task<IActionResult> PaystackWebhook()
    {
        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();
        var body = await new StreamReader(Request.Body).ReadToEndAsync();

        if (!VerifyPaystackSignature(body, signature))
            return Unauthorized();

        _logger.LogInformation("Paystack webhook received: {Body}", body);
        // Process payment logic here

        return Ok();
    }

    [HttpPost("stripe/webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var signature = Request.Headers["stripe-signature"].FirstOrDefault();
        var body = await new StreamReader(Request.Body).ReadToEndAsync();

        if (!VerifyStripeSignature(body, signature))
            return Unauthorized();

        _logger.LogInformation("Stripe webhook received: {Body}", body);
        // Process payment logic here

        return Ok();
    }

    private bool VerifyPaystackSignature(string body, string? signature)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        var secret = _configuration["Payment:Paystack:WebhookSecret"];
        var hash = ComputeHmacSha512(body, secret!);
        return hash.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    private bool VerifyStripeSignature(string body, string? signature)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        var secret = _configuration["Payment:Stripe:WebhookSecret"];
        var hash = ComputeHmacSha256(body, secret!);
        return signature.Contains(hash);
    }

    private static string ComputeHmacSha512(string data, string key)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
