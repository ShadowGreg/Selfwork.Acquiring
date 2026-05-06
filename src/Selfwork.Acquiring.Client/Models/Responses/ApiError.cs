using System.Text.Json.Serialization;

namespace Selfwork.Acquiring.Client.Models.Responses;

/// <summary>Error payload returned by the API on non-success responses.</summary>
public sealed class ApiError
{
    /// <summary>Machine-readable error code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>Human-readable error description.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
