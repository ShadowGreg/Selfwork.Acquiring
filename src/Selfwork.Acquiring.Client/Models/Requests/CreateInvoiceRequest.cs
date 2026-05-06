using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Selfwork.Acquiring.Client.Models.Requests;

/// <summary>Request payload for creating a new payment invoice.</summary>
public sealed class CreateInvoiceRequest
{
    /// <summary>Payment amount in kopecks (RUB × 100).</summary>
    [Required]
    [Range(1, int.MaxValue)]
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    /// <summary>Human-readable description of the payment.</summary>
    [Required]
    [MaxLength(255)]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>URL to redirect the customer after successful payment.</summary>
    [Url]
    [JsonPropertyName("success_url")]
    public string? SuccessUrl { get; set; }

    /// <summary>URL to redirect the customer after failed or cancelled payment.</summary>
    [Url]
    [JsonPropertyName("fail_url")]
    public string? FailUrl { get; set; }

    /// <summary>Arbitrary string passed back in webhook and redirect for idempotency.</summary>
    [MaxLength(128)]
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>Invoice expiration time in minutes. Default is 60.</summary>
    [Range(1, 10080)]
    [JsonPropertyName("expire_minutes")]
    public int? ExpireMinutes { get; set; }
}
