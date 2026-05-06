namespace Selfwork.Acquiring.Client.Webhook;

/// <summary>Verifies the HMAC-SHA256 signature on incoming webhook requests.</summary>
public interface IWebhookVerifier
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="signature"/> matches the HMAC-SHA256
    /// computed over the canonical payload string using the configured webhook secret.
    /// </summary>
    bool IsValid(PaymentWebhookPayload payload, string signature);

    /// <summary>
    /// Asserts the signature is valid; throws <see cref="Exceptions.WebhookVerificationException"/>
    /// when it is not.
    /// </summary>
    void AssertValid(PaymentWebhookPayload payload, string signature);
}
