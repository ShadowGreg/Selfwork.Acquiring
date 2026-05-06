using System.Text.Json.Serialization;
using Selfwork.Acquiring.Client.Models.Responses;

namespace Selfwork.Acquiring.Client.Webhook;

/// <summary>Payload delivered by Selfwork to your webhook endpoint on payment events.</summary>
public sealed class PaymentWebhookPayload
{
    /// <summary>Invoice identifier.</summary>
    [JsonPropertyName("invoice_id")]
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>New status of the invoice.</summary>
    [JsonPropertyName("status")]
    public InvoiceStatus Status { get; set; }

    /// <summary>Payment amount in kopecks.</summary>
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    /// <summary>Caller-supplied order identifier, if present.</summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>UTC timestamp of the event.</summary>
    [JsonPropertyName("event_at")]
    public DateTimeOffset EventAt { get; set; }
}
