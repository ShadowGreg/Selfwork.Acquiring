using System.Text.Json.Serialization;

namespace Selfwork.Acquiring.Client.Models.Responses;

/// <summary>Response from the API key verification endpoint.</summary>
public sealed class VerifyResponse
{
    /// <summary>Whether the API key is valid and active.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Name or identifier of the account tied to the key.</summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>UTC timestamp when verification was performed.</summary>
    [JsonPropertyName("verified_at")]
    public DateTimeOffset VerifiedAt { get; set; }
}
