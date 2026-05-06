using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Selfwork.Acquiring.Client.Exceptions;
using Selfwork.Acquiring.Client.Extensions;
using Selfwork.Acquiring.Client.Models.Responses;
using Selfwork.Acquiring.Client.Webhook;

namespace Selfwork.Acquiring.Tests.Unit;

/// <summary>
/// Unit tests for IWebhookVerifier (HMAC-SHA256 signature validation).
/// All tests follow the Arrange / Act / Assert (AAA) pattern.
/// </summary>
public sealed class WebhookVerifierTests
{
    private const string Secret = "test-secret-key";

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IWebhookVerifier CreateVerifier(string secret = Secret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Selfwork:BaseUrl"] = "https://api.test",
                ["Selfwork:ApiKey"] = "key",
                ["Selfwork:WebhookSecret"] = secret,
            })
            .Build();
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSelfworkAcquiring(config);
        return services.BuildServiceProvider().GetRequiredService<IWebhookVerifier>();
    }

    private static string ComputeSignature(PaymentWebhookPayload payload, string secret = Secret)
    {
        var data = $"{payload.InvoiceId}:{payload.Status.ToString().ToLowerInvariant()}:{payload.Amount}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }

    private static PaymentWebhookPayload MakePayload(
        string invoiceId = "inv-123",
        InvoiceStatus status = InvoiceStatus.Paid,
        int amount = 10000) => new()
    {
        InvoiceId = invoiceId,
        Status = status,
        Amount = amount,
        EventAt = DateTimeOffset.UtcNow,
    };

    // ── IsValid ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_CorrectSignature_ReturnsTrue()
    {
        // Arrange
        var verifier = CreateVerifier();
        var payload = MakePayload();
        var signature = ComputeSignature(payload);

        // Act
        var result = verifier.IsValid(payload, signature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WrongSignature_ReturnsFalse()
    {
        // Arrange
        var verifier = CreateVerifier();
        var payload = MakePayload();

        // Act
        var result = verifier.IsValid(payload, "deadbeef");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_TamperedAmount_ReturnsFalse()
    {
        // Arrange
        var verifier = CreateVerifier();
        var payload = MakePayload(amount: 10000);
        var signature = ComputeSignature(payload);
        payload.Amount = 1; // tamper after signing

        // Act
        var result = verifier.IsValid(payload, signature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_UpperCaseSignature_TreatedAsCaseInsensitive()
    {
        // Arrange
        var verifier = CreateVerifier();
        var payload = MakePayload();
        var upperSig = ComputeSignature(payload).ToUpperInvariant();

        // Act
        var result = verifier.IsValid(payload, upperSig);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_NullPayload_ThrowsArgumentNullException()
    {
        // Arrange
        var verifier = CreateVerifier();

        // Act
        var act = () => verifier.IsValid(null!, "sig");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_EmptySignature_ThrowsArgumentException()
    {
        // Arrange
        var verifier = CreateVerifier();
        var payload = MakePayload();

        // Act
        var act = () => verifier.IsValid(payload, "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Expired)]
    [InlineData(InvoiceStatus.Cancelled)]
    [InlineData(InvoiceStatus.Failed)]
    public void IsValid_AllInvoiceStatuses_SignatureIsValid(InvoiceStatus status)
    {
        // Arrange
        var verifier = CreateVerifier();
        var payload = MakePayload(status: status);
        var signature = ComputeSignature(payload);

        // Act
        var result = verifier.IsValid(payload, signature);

        // Assert
        result.Should().BeTrue();
    }

    // ── AssertValid ───────────────────────────────────────────────────────────

    [Fact]
    public void AssertValid_InvalidSignature_ThrowsWebhookVerificationException()
    {
        // Arrange
        var verifier = CreateVerifier();
        var payload = MakePayload();

        // Act
        var act = () => verifier.AssertValid(payload, "bad-sig");

        // Assert
        act.Should().Throw<WebhookVerificationException>()
           .WithMessage("*inv-123*");
    }

    [Fact]
    public void AssertValid_ValidSignature_DoesNotThrow()
    {
        // Arrange
        var verifier = CreateVerifier();
        var payload = MakePayload();
        var signature = ComputeSignature(payload);

        // Act
        var act = () => verifier.AssertValid(payload, signature);

        // Assert
        act.Should().NotThrow();
    }
}
