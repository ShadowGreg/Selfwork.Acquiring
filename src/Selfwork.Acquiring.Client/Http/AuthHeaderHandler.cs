using Microsoft.Extensions.Options;
using Selfwork.Acquiring.Client.Configuration;

namespace Selfwork.Acquiring.Client.Http;

/// <summary>Injects the X-Api-Key header into every outgoing request.</summary>
internal sealed class AuthHeaderHandler(IOptions<SelfworkOptions> options) : DelegatingHandler
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly string _apiKey = options.Value.ApiKey;

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(ApiKeyHeader, _apiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
