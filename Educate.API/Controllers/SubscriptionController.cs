using System.Security.Claims;
using Educate.Application.Subscriptions.Queries.CheckSubscriptionEligibility;
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

    /// <summary>
    /// Retrieves current user's active subscriptions
    /// </summary>
    [HttpGet("my-subscriptions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubscriptions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subscriptions = await _mediator.Send(new GetUserSubscriptionsQuery(userId));
        return Ok(subscriptions);
    }

    /// <summary>
    /// Checks if user is eligible to subscribe to a level
    /// </summary>
    [HttpGet("eligibility/{levelId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckEligibility(Guid levelId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var eligibility = await _mediator.Send(new CheckSubscriptionEligibilityQuery(userId, levelId));
        return Ok(eligibility);
    }

    /// <summary>
    /// Gets specific subscription status for a level
    /// </summary>
    [HttpGet("status/{levelId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionStatus(Guid levelId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var eligibility = await _mediator.Send(new CheckSubscriptionEligibilityQuery(userId, levelId));
        
        if (eligibility.ExistingSubscription == null)
            return NotFound(new { message = "No subscription found for this level" });

        return Ok(eligibility.ExistingSubscription);
    }

    /// <summary>
    /// Subscribes to a course level
    /// </summary>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new Educate.Application.Courses.Commands.SubscribeToLevel.SubscribeToLevelCommand(request.LevelId, userId));
            if (!result)
                return BadRequest("Already subscribed or level not found");

            return Ok(new { message = "Subscription created successfully" });
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }
}

public record SubscribeRequest(Guid LevelId);
