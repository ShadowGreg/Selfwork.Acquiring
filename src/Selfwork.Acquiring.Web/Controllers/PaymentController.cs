using Microsoft.AspNetCore.Mvc;
using Selfwork.Acquiring.Client.Exceptions;
using Selfwork.Acquiring.Client.Models.Requests;
using Selfwork.Acquiring.Client.Services;
using Selfwork.Acquiring.Web.Models;

namespace Selfwork.Acquiring.Web.Controllers;

/// <summary>Payment lifecycle operations: create, query, cancel.</summary>
[ApiController]
[Route("api/payment")]
[Produces("application/json")]
public sealed class PaymentController(
    IAcquiringService acquiring,
    ILogger<PaymentController> logger) : ControllerBase
{
    /// <summary>Verify connectivity to the Selfwork API.</summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        try
        {
            var result = await acquiring.VerifyAsync(ct);
            return Ok(new { connected = result.Success, account = result.Account, verifiedAt = result.VerifiedAt });
        }
        catch (AcquiringApiException ex)
        {
            logger.LogError(ex, "Selfwork API health check failed");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    /// <summary>Create a new payment invoice and return the payment URL.</summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest body, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var invoice = await acquiring.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                Amount = body.Amount,
                Description = body.Description,
                OrderId = body.OrderId,
                SuccessUrl = body.SuccessUrl,
                FailUrl = body.FailUrl,
            }, ct);

            var response = PaymentResponse.From(invoice);
            return CreatedAtAction(nameof(Status), new { id = invoice.Id }, response);
        }
        catch (AcquiringApiException ex)
        {
            logger.LogError(ex, "Failed to create invoice");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message, code = ex.ErrorCode });
        }
    }

    /// <summary>Return the current status of an invoice.</summary>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Status(string id, CancellationToken ct)
    {
        try
        {
            var invoice = await acquiring.GetInvoiceAsync(id, ct);
            return Ok(PaymentResponse.From(invoice));
        }
        catch (AcquiringApiException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { error = $"Invoice '{id}' not found" });
        }
        catch (AcquiringApiException ex)
        {
            logger.LogError(ex, "Failed to get invoice {InvoiceId}", id);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    /// <summary>Cancel a pending invoice.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        try
        {
            await acquiring.CancelInvoiceAsync(id, ct);
            return NoContent();
        }
        catch (AcquiringApiException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { error = $"Invoice '{id}' not found" });
        }
        catch (AcquiringApiException ex)
        {
            logger.LogError(ex, "Failed to cancel invoice {InvoiceId}", id);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }
}
