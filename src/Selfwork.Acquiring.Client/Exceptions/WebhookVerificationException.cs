namespace Selfwork.Acquiring.Client.Exceptions;

/// <summary>Thrown when a webhook payload fails HMAC signature verification.</summary>
public sealed class WebhookVerificationException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public WebhookVerificationException(string message) : base(message) { }
}
