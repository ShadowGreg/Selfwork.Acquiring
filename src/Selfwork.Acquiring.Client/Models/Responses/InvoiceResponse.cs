using System.Text.Json.Serialization;

namespace Selfwork.Acquiring.Client.Models.Responses;

/// <summary>Represents a single payment invoice returned by the API.</summary>
public sealed class InvoiceResponse
{
    /// <summary>Unique invoice identifier assigned by Selfwork.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Current invoice status.</summary>
    [JsonPropertyName("status")]
    public InvoiceStatus Status { get; set; }

    /// <summary>Payment amount in kopecks.</summary>
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    /// <summary>Invoice description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Caller-supplied order identifier, if provided at creation.</summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>URL where the customer should be redirected to complete payment.</summary>
    [JsonPropertyName("payment_url")]
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>UTC time when the invoice was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC time when the invoice expires (or expired).</summary>
    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>UTC time when payment was confirmed, null if not yet paid.</summary>
    [JsonPropertyName("paid_at")]
    public DateTimeOffset? PaidAt { get; set; }
}
