using Selfwork.Acquiring.Client.Models.Responses;

namespace Selfwork.Acquiring.Web.Models;

/// <summary>HTTP response for payment operations.</summary>
public sealed class PaymentResponse
{
    /// <summary>Selfwork invoice identifier.</summary>
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>Current status of the invoice.</summary>
    public InvoiceStatus Status { get; set; }

    /// <summary>Amount in kopecks.</summary>
    public int Amount { get; set; }

    /// <summary>URL to redirect customer to complete payment. Null for status-only responses.</summary>
    public string? PaymentUrl { get; set; }

    /// <summary>Your internal order reference.</summary>
    public string? OrderId { get; set; }

    /// <summary>UTC time the invoice was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC time the invoice expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    internal static PaymentResponse From(InvoiceResponse invoice) => new()
    {
        InvoiceId = invoice.Id,
        Status = invoice.Status,
        Amount = invoice.Amount,
        PaymentUrl = invoice.PaymentUrl,
        OrderId = invoice.OrderId,
        CreatedAt = invoice.CreatedAt,
        ExpiresAt = invoice.ExpiresAt,
    };
}
