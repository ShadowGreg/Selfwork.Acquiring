using System.Text.Json.Serialization;

namespace Selfwork.Acquiring.Client.Models.Responses;

/// <summary>Paginated list of invoices.</summary>
public sealed class InvoiceListResponse
{
    /// <summary>Items on the current page.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<InvoiceResponse> Items { get; set; } = [];

    /// <summary>Total number of invoices matching the query.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Current page number (1-based).</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>Number of items per page.</summary>
    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }
}
