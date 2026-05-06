using System.ComponentModel.DataAnnotations;

namespace Selfwork.Acquiring.Web.Models;

/// <summary>HTTP request body for creating a payment.</summary>
public sealed class CreatePaymentRequest
{
    /// <summary>Amount in kopecks (RUB × 100). Must be ≥ 1.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }

    /// <summary>Human-readable payment description.</summary>
    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Your internal order reference.</summary>
    [MaxLength(128)]
    public string? OrderId { get; set; }

    /// <summary>Redirect URL on successful payment.</summary>
    [Url]
    public string? SuccessUrl { get; set; }

    /// <summary>Redirect URL on failed or cancelled payment.</summary>
    [Url]
    public string? FailUrl { get; set; }
}
