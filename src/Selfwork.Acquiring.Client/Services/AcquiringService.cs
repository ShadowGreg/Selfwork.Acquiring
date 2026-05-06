using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Selfwork.Acquiring.Client.Exceptions;
using Selfwork.Acquiring.Client.Models.Requests;
using Selfwork.Acquiring.Client.Models.Responses;

namespace Selfwork.Acquiring.Client.Services;

/// <inheritdoc />
internal sealed class AcquiringService(
    HttpClient httpClient,
    ILogger<AcquiringService> logger) : IAcquiringService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<VerifyResponse> VerifyAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Verifying Selfwork API key");
        return await GetAsync<VerifyResponse>("/acquiring/verify", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<InvoiceResponse> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        logger.LogInformation("Creating invoice: amount={Amount} orderId={OrderId}",
            request.Amount, request.OrderId);

        using var response = await httpClient.PostAsJsonAsync(
            "/acquiring/invoice", request, JsonOptions, cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<InvoiceResponse>(JsonOptions, cancellationToken)
                     ?? throw new AcquiringApiException(500, "Empty response body from CreateInvoice");

        logger.LogInformation("Invoice created: id={Id} status={Status}", result.Id, result.Status);
        return result;
    }

    /// <inheritdoc />
    public async Task<InvoiceResponse> GetInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);
        logger.LogInformation("Getting invoice status: id={InvoiceId}", invoiceId);
        return await GetAsync<InvoiceResponse>($"/acquiring/invoice/{Uri.EscapeDataString(invoiceId)}", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<InvoiceListResponse> ListInvoicesAsync(
        InvoiceListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var qs = query?.ToQueryString() ?? string.Empty;
        var path = string.IsNullOrEmpty(qs) ? "/acquiring/invoices" : $"/acquiring/invoices?{qs}";
        logger.LogInformation("Listing invoices: {Query}", qs);
        return await GetAsync<InvoiceListResponse>(path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);
        logger.LogInformation("Cancelling invoice: id={InvoiceId}", invoiceId);

        using var response = await httpClient.DeleteAsync(
            $"/acquiring/invoice/{Uri.EscapeDataString(invoiceId)}", cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
        logger.LogInformation("Invoice cancelled: id={InvoiceId}", invoiceId);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
               ?? throw new AcquiringApiException(500, $"Empty response body from GET {path}");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var statusCode = (int)response.StatusCode;
        string? errorCode = null;
        string message;

        try
        {
            var error = await response.Content.ReadFromJsonAsync<Models.Responses.ApiError>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
            errorCode = error?.Code;
            message = error?.Message ?? response.ReasonPhrase ?? "Unknown API error";
        }
        catch
        {
            message = response.ReasonPhrase ?? $"HTTP {statusCode}";
        }

        throw new AcquiringApiException(statusCode, message, errorCode);
    }
}
