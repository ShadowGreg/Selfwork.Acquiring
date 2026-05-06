using System.Text.Json.Serialization;
using Selfwork.Acquiring.Client.Models.Responses;

namespace Selfwork.Acquiring.Client.Models.Requests;

/// <summary>Query parameters for listing invoices.</summary>
public sealed class InvoiceListQuery
{
    /// <summary>Filter by invoice status.</summary>
    [JsonPropertyName("status")]
    public InvoiceStatus? Status { get; set; }

    /// <summary>Page number (1-based). Default: 1.</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    /// <summary>Number of items per page (max 100). Default: 20.</summary>
    [JsonPropertyName("per_page")]
    public int PerPage { get; set; } = 20;

    /// <summary>Filter invoices created after this date (UTC).</summary>
    [JsonPropertyName("from")]
    public DateTimeOffset? From { get; set; }

    /// <summary>Filter invoices created before this date (UTC).</summary>
    [JsonPropertyName("to")]
    public DateTimeOffset? To { get; set; }

    internal string ToQueryString()
    {
        var parts = new List<string>();
        if (Status.HasValue) parts.Add($"status={Status.Value.ToString().ToLowerInvariant()}");
        parts.Add($"page={Page}");
        parts.Add($"per_page={PerPage}");
        if (From.HasValue) parts.Add($"from={Uri.EscapeDataString(From.Value.ToString("O"))}");
        if (To.HasValue) parts.Add($"to={Uri.EscapeDataString(To.Value.ToString("O"))}");
        return string.Join("&", parts);
    }
}
