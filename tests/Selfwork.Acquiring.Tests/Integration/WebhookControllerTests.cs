using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Selfwork.Acquiring.Client.Exceptions;
using Selfwork.Acquiring.Client.Models.Responses;
using Selfwork.Acquiring.Client.Webhook;

namespace Selfwork.Acquiring.Tests.Integration;

public sealed class WebhookControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string WebhookSecret = "integration-test-secret";
    private readonly HttpClient _client;
    private readonly Mock<IWebhookVerifier> _verifierMock = new();

    public WebhookControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real verifier with controllable mock
                var descriptor = services.Single(d => d.ServiceType == typeof(IWebhookVerifier));
                services.Remove(descriptor);
                services.AddSingleton(_verifierMock.Object);
            });
            // RetryCount=0 + short timeout to skip startup connectivity delay
            builder.UseSetting("Selfwork:ApiKey", "test-key");
            builder.UseSetting("Selfwork:WebhookSecret", WebhookSecret);
            builder.UseSetting("Selfwork:BaseUrl", "https://api.test");
            builder.UseSetting("Selfwork:RetryCount", "0");
            builder.UseSetting("Selfwork:TimeoutSeconds", "2");
        }).CreateClient();
    }

    private static PaymentWebhookPayload MakePaidPayload() => new()
    {
        InvoiceId = "inv-webhook-001",
        Status = InvoiceStatus.Paid,
        Amount = 15000,
        OrderId = "order-42",
        EventAt = DateTimeOffset.UtcNow,
    };

    private static string ComputeSignature(PaymentWebhookPayload p, string secret = WebhookSecret)
    {
        var data = $"{p.InvoiceId}:{p.Status.ToString().ToLowerInvariant()}:{p.Amount}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }

    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandlePayment_ValidSignature_Returns200()
    {
        // Arrange
        var payload = MakePaidPayload();
        _verifierMock.Setup(v => v.AssertValid(It.IsAny<PaymentWebhookPayload>(), It.IsAny<string>()));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/payment")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Webhook-Signature", ComputeSignature(payload));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HandlePayment_InvalidSignature_Returns401()
    {
        // Arrange
        var payload = MakePaidPayload();
        _verifierMock.Setup(v => v.AssertValid(It.IsAny<PaymentWebhookPayload>(), It.IsAny<string>()))
                     .Throws(new WebhookVerificationException("Invalid signature"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/payment")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Webhook-Signature", "bad-signature");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandlePayment_MissingSignatureHeader_Returns401WithMessage()
    {
        // Arrange
        var payload = MakePaidPayload();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/payment")
        {
            Content = JsonContent.Create(payload),
        };
        // No X-Webhook-Signature header

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Contain("Missing");
    }

    [Fact]
    public async Task HandlePayment_ValidSignature_CallsAssertValidOnVerifier()
    {
        // Arrange
        var payload = MakePaidPayload();
        var sig = ComputeSignature(payload);
        _verifierMock.Setup(v => v.AssertValid(It.IsAny<PaymentWebhookPayload>(), sig));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/payment")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Webhook-Signature", sig);

        // Act
        await _client.SendAsync(request);

        // Assert
        _verifierMock.Verify(v => v.AssertValid(It.IsAny<PaymentWebhookPayload>(), sig), Times.Once);
    }
}
