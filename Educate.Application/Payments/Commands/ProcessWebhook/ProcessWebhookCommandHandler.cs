using System.Text.Json;
using Educate.Application.Interfaces;
using Educate.Application.Payments.Commands.VerifyPayment;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Educate.Application.Payments.Commands.ProcessWebhook;

public class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand, bool>
{
    private readonly IPaymentService _paymentService;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessWebhookCommandHandler> _logger;

    public ProcessWebhookCommandHandler(
        IPaymentService paymentService, 
        IMediator mediator,
        ILogger<ProcessWebhookCommandHandler> logger)
    {
        _paymentService = paymentService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Signature
        if (!_paymentService.ValidateSignature(request.Body, request.Signature))
        {
            _logger.LogWarning("Invalid Paystack Webhook Signature");
            return false;
        }

        try
        {
            // 2. Parse Event
            using var doc = JsonDocument.Parse(request.Body);
            var root = doc.RootElement;

            if (root.TryGetProperty("event", out var eventElement) && eventElement.GetString() == "charge.success")
            {
                if (root.TryGetProperty("data", out var dataElement) && dataElement.TryGetProperty("reference", out var referenceElement))
                {
                    var reference = referenceElement.GetString();
                    if (!string.IsNullOrEmpty(reference))
                    {
                        // 3. Trigger Verification (which also activates subscription)
                        var result = await _mediator.Send(new VerifyPaymentCommand(reference), cancellationToken);
                        if (result.Status)
                        {
                            _logger.LogInformation("Webhook processed successfully for reference: {Reference}", reference);
                            return true;
                        }
                        else
                        {
                            _logger.LogWarning("Webhook verification failed for reference: {Reference}", reference);
                             // Return true anyway to ACK Paystack, as retrying won't change invalid status unless it's a transient error.
                             // But usually we return 200.
                            return true; 
                        }
                    }
                }
            }
            else
            {
                // Ignore other events
                return true;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing webhook body");
            return false;
        }

        return true;
    }
}
