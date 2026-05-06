using System.ComponentModel.DataAnnotations;

namespace Selfwork.Acquiring.Client.Configuration;

/// <summary>Configuration options for the Selfwork Acquiring API client.</summary>
public sealed class SelfworkOptions
{
    /// <summary>Configuration section name in appsettings.</summary>
    public const string Section = "Selfwork";

    /// <summary>Base URL of the Selfwork API (no trailing slash).</summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://api.selfwork.ru";

    /// <summary>API key issued in the Selfwork dashboard.</summary>
    [Required]
    [MinLength(1)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Secret used to verify incoming webhook signatures.</summary>
    [Required]
    [MinLength(1)]
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Number of retry attempts on transient errors. Default: 3.</summary>
    [Range(0, 10)]
    public int RetryCount { get; set; } = 3;

    /// <summary>HTTP request timeout in seconds. Default: 30.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
