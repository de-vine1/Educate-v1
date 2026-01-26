using System.Security.Claims;
using Educate.Application.Payments.Commands.InitializePayment;
using Educate.Application.Payments.Commands.VerifyPayment;
using Educate.Application.Payments.Commands.ProcessWebhook;
using Educate.Application.Payments.Queries.GetPaymentHistory;
using Educate.Application.Models.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Initializes a new payment transaction
    /// </summary>
    /// <param name="request">Payment initialization details</param>
    /// <returns>Initialization response with payment reference</returns>
    [HttpPost("initialize")]
    [ProducesResponseType(typeof(PaymentInitializationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentInitializationResponse>> InitializePayment([FromBody] InitializePaymentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var command = new InitializePaymentCommand(userId, request.LevelId);
        var result = await _mediator.Send(command);

        if (!result.Status)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Verifies a payment transaction
    /// </summary>
    /// <param name="reference">Payment reference</param>
    /// <returns>Verification result</returns>
    [HttpGet("verify/{reference}")]
    [ProducesResponseType(typeof(PaymentVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentVerificationResponse>> VerifyPayment(string reference)
    {
        var result = await _mediator.Send(new VerifyPaymentCommand(reference));
        
        if (!result.Status)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves payment history for the current user
    /// </summary>
    /// <returns>List of past payments</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var history = await _mediator.Send(new GetPaymentHistoryQuery(userId));
        return Ok(history);
    }

    /// <summary>
    /// Paystack webhook endpoint
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook()
    {
        var signature = Request.Headers["x-paystack-signature"].ToString();
        
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var command = new ProcessWebhookCommand(body, signature);
        var result = await _mediator.Send(command);

        if (!result)
            return BadRequest();

        return Ok();
    }
}

public record InitializePaymentRequest(Guid LevelId);
