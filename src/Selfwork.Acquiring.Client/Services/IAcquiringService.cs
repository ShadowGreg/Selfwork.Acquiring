using Selfwork.Acquiring.Client.Models.Requests;
using Selfwork.Acquiring.Client.Models.Responses;

namespace Selfwork.Acquiring.Client.Services;

/// <summary>Provides access to all Selfwork Acquiring API operations.</summary>
public interface IAcquiringService
{
    /// <summary>Verifies that the configured API key is valid and active.</summary>
    Task<VerifyResponse> VerifyAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a new payment invoice and returns the payment URL.</summary>
    Task<InvoiceResponse> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current state of an invoice by its identifier.</summary>
    Task<InvoiceResponse> GetInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a paginated list of invoices matching the given query.</summary>
    Task<InvoiceListResponse> ListInvoicesAsync(
        InvoiceListQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a pending invoice. Has no effect on already paid or expired invoices.</summary>
    Task CancelInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default);
}
