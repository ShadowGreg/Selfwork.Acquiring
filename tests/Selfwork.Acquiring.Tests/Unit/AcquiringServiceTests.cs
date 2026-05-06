using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Selfwork.Acquiring.Client.Exceptions;
using Selfwork.Acquiring.Client.Extensions;
using Selfwork.Acquiring.Client.Models.Requests;
using Selfwork.Acquiring.Client.Models.Responses;
using Selfwork.Acquiring.Client.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Selfwork.Acquiring.Tests.Unit;

/// <summary>
/// Unit tests for IAcquiringService backed by WireMock.Net HTTP stub.
/// All tests follow the Arrange / Act / Assert (AAA) pattern.
/// RetryCount=0 ensures tests are fast and deterministic.
/// </summary>
public sealed class AcquiringServiceTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly IAcquiringService _svc;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AcquiringServiceTests()
    {
        _server = WireMockServer.Start();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Selfwork:BaseUrl"] = _server.Urls[0],
                ["Selfwork:ApiKey"] = "test-key",
                ["Selfwork:WebhookSecret"] = "test-secret",
                ["Selfwork:RetryCount"] = "0",
                ["Selfwork:TimeoutSeconds"] = "5",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSelfworkAcquiring(config);
        _svc = services.BuildServiceProvider().GetRequiredService<IAcquiringService>();
    }

    public void Dispose() => _server.Dispose();

    // ── Stub helpers ─────────────────────────────────────────────────────────

    private void StubGet(string path, object body, int status = 200) =>
        _server.Given(Request.Create().WithPath(path).UsingGet())
               .RespondWith(Response.Create()
                   .WithStatusCode(status)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(JsonSerializer.Serialize(body, JsonOptions)));

    private void StubPost(string path, object body, int status = 200) =>
        _server.Given(Request.Create().WithPath(path).UsingPost())
               .RespondWith(Response.Create()
                   .WithStatusCode(status)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(JsonSerializer.Serialize(body, JsonOptions)));

    private void StubDelete(string path, int status = 204) =>
        _server.Given(Request.Create().WithPath(path).UsingDelete())
               .RespondWith(Response.Create().WithStatusCode(status));

    private void StubDeleteError(string path, int status, string code, string message) =>
        _server.Given(Request.Create().WithPath(path).UsingDelete())
               .RespondWith(Response.Create()
                   .WithStatusCode(status)
                   .WithHeader("Content-Type", "application/json")
                   .WithBody(JsonSerializer.Serialize(new { code, message }, JsonOptions)));

    private void StubError(string method, string path, int status, string code, string message)
    {
        var req = method switch
        {
            "GET" => Request.Create().WithPath(path).UsingGet(),
            "POST" => Request.Create().WithPath(path).UsingPost(),
            _ => throw new ArgumentException(method),
        };
        _server.Given(req).RespondWith(Response.Create()
            .WithStatusCode(status)
            .WithHeader("Content-Type", "application/json")
            .WithBody(JsonSerializer.Serialize(new { code, message }, JsonOptions)));
    }

    // ── VerifyAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_ApiReturnsSuccess_MapsToVerifyResponse()
    {
        // Arrange
        StubGet("/acquiring/verify", new
        {
            success = true,
            account = "acct-1",
            verified_at = DateTimeOffset.UtcNow,
        });

        // Act
        var result = await _svc.VerifyAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Account.Should().Be("acct-1");
    }

    [Fact]
    public async Task VerifyAsync_ApiReturns401_ThrowsAcquiringApiExceptionWith401AndErrorCode()
    {
        // Arrange
        StubError("GET", "/acquiring/verify", 401, "INVALID_KEY", "Bad API key");

        // Act
        var act = () => _svc.VerifyAsync();

        // Assert
        await act.Should().ThrowAsync<AcquiringApiException>()
                 .Where(ex => ex.StatusCode == 401 && ex.ErrorCode == "INVALID_KEY");
    }

    // ── CreateInvoiceAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateInvoiceAsync_ValidRequest_ReturnsInvoiceWithPaymentUrl()
    {
        // Arrange
        StubPost("/acquiring/invoice", new
        {
            id = "inv-001",
            status = "pending",
            amount = 5000,
            description = "Test payment",
            payment_url = "https://pay.selfwork.ru/inv-001",
            created_at = DateTimeOffset.UtcNow,
            expires_at = DateTimeOffset.UtcNow.AddHours(1),
        });

        // Act
        var result = await _svc.CreateInvoiceAsync(new CreateInvoiceRequest
        {
            Amount = 5000,
            Description = "Test payment",
        });

        // Assert
        result.Id.Should().Be("inv-001");
        result.PaymentUrl.Should().StartWith("https://");
        result.Status.Should().Be(InvoiceStatus.Pending);
    }

    [Fact]
    public async Task CreateInvoiceAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange — no stub needed, guard throws before HTTP

        // Act
        var act = () => _svc.CreateInvoiceAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateInvoiceAsync_ApiReturns500_ThrowsAcquiringApiExceptionWith500()
    {
        // Arrange
        StubError("POST", "/acquiring/invoice", 500, "SERVER_ERROR", "Internal server error");

        // Act
        var act = () => _svc.CreateInvoiceAsync(new CreateInvoiceRequest { Amount = 100, Description = "x" });

        // Assert
        await act.Should().ThrowAsync<AcquiringApiException>()
                 .Where(ex => ex.StatusCode == 500 && ex.ErrorCode == "SERVER_ERROR");
    }

    // ── GetInvoiceAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetInvoiceAsync_ExistingPaidInvoice_ReturnsPaidStatus()
    {
        // Arrange
        StubGet("/acquiring/invoice/inv-002", new
        {
            id = "inv-002",
            status = "paid",
            amount = 1000,
            paid_at = DateTimeOffset.UtcNow,
        });

        // Act
        var result = await _svc.GetInvoiceAsync("inv-002");

        // Assert
        result.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task GetInvoiceAsync_NonExistentInvoice_ThrowsAcquiringApiExceptionWith404()
    {
        // Arrange
        StubError("GET", "/acquiring/invoice/missing", 404, "NOT_FOUND", "Invoice not found");

        // Act
        var act = () => _svc.GetInvoiceAsync("missing");

        // Assert
        await act.Should().ThrowAsync<AcquiringApiException>()
                 .Where(ex => ex.StatusCode == 404);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetInvoiceAsync_BlankId_ThrowsArgumentException(string id)
    {
        // Arrange — no stub needed, guard throws before HTTP

        // Act
        var act = () => _svc.GetInvoiceAsync(id);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── CancelInvoiceAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelInvoiceAsync_PendingInvoice_CompletesWithoutException()
    {
        // Arrange
        StubDelete("/acquiring/invoice/inv-003");

        // Act
        var act = () => _svc.CancelInvoiceAsync("inv-003");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CancelInvoiceAsync_AlreadyPaidInvoice_ThrowsAcquiringApiExceptionWith409()
    {
        // Arrange
        StubDeleteError("/acquiring/invoice/inv-004", 409, "ALREADY_PAID", "Invoice already paid");

        // Act
        var act = () => _svc.CancelInvoiceAsync("inv-004");

        // Assert
        await act.Should().ThrowAsync<AcquiringApiException>()
                 .Where(ex => ex.StatusCode == 409);
    }

    // ── ListInvoicesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListInvoicesAsync_NoQuery_ReturnsPaginatedFirstPage()
    {
        // Arrange
        StubGet("/acquiring/invoices", new
        {
            items = new[] { new { id = "inv-005", status = "pending", amount = 500 } },
            total = 1,
            page = 1,
            per_page = 20,
        });

        // Act
        var result = await _svc.ListInvoicesAsync();

        // Assert
        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
        result.Page.Should().Be(1);
    }
}
