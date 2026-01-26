using System.Security.Claims;
using Educate.Application.Payments.Commands.ProcessPayment;
using Educate.Application.Payments.Queries.GetPaymentHistory;
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

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var command = new ProcessPaymentCommand(userId, request.CourseId, request.CardToken);
            var transactionId = await _mediator.Send(command);
            return Ok(new { success = true, transactionId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var history = await _mediator.Send(new GetPaymentHistoryQuery(userId));
        return Ok(history);
    }
}

public record ProcessPaymentRequest(int CourseId, string CardToken);
