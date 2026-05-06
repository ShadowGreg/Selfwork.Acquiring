using Microsoft.AspNetCore.Mvc;
using Selfwork.Acquiring.Client.Exceptions;
using Selfwork.Acquiring.Client.Models.Responses;
using Selfwork.Acquiring.Client.Webhook;

namespace Selfwork.Acquiring.Web.Controllers;

/// <summary>Receives and processes payment event webhooks from Selfwork.</summary>
[ApiController]
[Route("api/webhook")]
[Produces("application/json")]
public sealed class WebhookController(
    IWebhookVerifier verifier,
    ILogger<WebhookController> logger) : ControllerBase
{
    private const string SignatureHeader = "X-Webhook-Signature";

    /// <summary>
    /// Endpoint that Selfwork calls when invoice status changes.
    /// Must respond with 2xx within 10 seconds or Selfwork will retry.
    /// </summary>
    [HttpPost("payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult HandlePayment(
        [FromBody] PaymentWebhookPayload payload,
        [FromHeader(Name = SignatureHeader)] string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            logger.LogWarning("Webhook received without signature header");
            return Unauthorized(new { error = "Missing signature header" });
        }

        try
        {
            verifier.AssertValid(payload, signature);
        }
        catch (WebhookVerificationException ex)
        {
            logger.LogWarning(ex, "Webhook signature invalid for invoice {InvoiceId}", payload.InvoiceId);
            return Unauthorized(new { error = "Invalid webhook signature" });
        }

        logger.LogInformation(
            "Webhook received: invoice={InvoiceId} status={Status} amount={Amount}",
            payload.InvoiceId, payload.Status, payload.Amount);

        ProcessWebhookEvent(payload);
        return Ok();
    }

    private void ProcessWebhookEvent(PaymentWebhookPayload payload)
    {
        switch (payload.Status)
        {
            case InvoiceStatus.Paid:
                logger.LogInformation("Payment confirmed for invoice {InvoiceId} orderId={OrderId}",
                    payload.InvoiceId, payload.OrderId);
                // TODO: fulfil order, send confirmation email, etc.
                break;

            case InvoiceStatus.Expired:
            case InvoiceStatus.Cancelled:
            case InvoiceStatus.Failed:
                logger.LogWarning("Payment not completed: invoice={InvoiceId} status={Status}",
                    payload.InvoiceId, payload.Status);
                // TODO: release reserved stock, notify customer, etc.
                break;

            default:
                logger.LogDebug("Unhandled webhook status {Status} for invoice {InvoiceId}",
                    payload.Status, payload.InvoiceId);
                break;
        }
    }
}
