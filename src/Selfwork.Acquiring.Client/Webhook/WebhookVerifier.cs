using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Selfwork.Acquiring.Client.Configuration;
using Selfwork.Acquiring.Client.Exceptions;

namespace Selfwork.Acquiring.Client.Webhook;

/// <inheritdoc />
internal sealed class WebhookVerifier(IOptions<SelfworkOptions> options) : IWebhookVerifier
{
    private readonly byte[] _secretBytes = Encoding.UTF8.GetBytes(options.Value.WebhookSecret);

    /// <inheritdoc />
    public bool IsValid(PaymentWebhookPayload payload, string signature)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);

        var canonical = BuildCanonical(payload);
        var expected = ComputeHmac(canonical);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(expected));
    }

    /// <inheritdoc />
    public void AssertValid(PaymentWebhookPayload payload, string signature)
    {
        if (!IsValid(payload, signature))
            throw new WebhookVerificationException(
                $"Webhook signature verification failed for invoice {payload.InvoiceId}.");
    }

    private string ComputeHmac(string data)
    {
        using var hmac = new HMACSHA256(_secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildCanonical(PaymentWebhookPayload p) =>
        $"{p.InvoiceId}:{p.Status.ToString().ToLowerInvariant()}:{p.Amount}";
}
