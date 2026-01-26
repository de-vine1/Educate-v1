using System.Security.Claims;
using Educate.Application.Subscriptions.Queries.GetUserSubscriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("my-subscriptions")]
    public async Task<IActionResult> GetMySubscriptions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subscriptions = await _mediator.Send(new GetUserSubscriptionsQuery(userId));
        return Ok(subscriptions);
    }
}
