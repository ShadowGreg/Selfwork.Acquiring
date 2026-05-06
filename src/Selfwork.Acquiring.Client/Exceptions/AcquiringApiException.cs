namespace Selfwork.Acquiring.Client.Exceptions;

/// <summary>Thrown when the Selfwork API returns a non-success HTTP response.</summary>
public sealed class AcquiringApiException : Exception
{
    /// <summary>HTTP status code returned by the API.</summary>
    public int StatusCode { get; }

    /// <summary>Error code from the API response body, if present.</summary>
    public string? ErrorCode { get; }

    /// <summary>Initializes a new instance.</summary>
    public AcquiringApiException(int statusCode, string message, string? errorCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    /// <summary>Initializes a new instance with an inner exception.</summary>
    public AcquiringApiException(int statusCode, string message, Exception innerException, string? errorCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
