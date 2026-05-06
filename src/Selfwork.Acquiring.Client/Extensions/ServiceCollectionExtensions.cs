using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Selfwork.Acquiring.Client.Configuration;
using Selfwork.Acquiring.Client.Http;
using Selfwork.Acquiring.Client.Services;
using Selfwork.Acquiring.Client.Webhook;

namespace Selfwork.Acquiring.Client.Extensions;

/// <summary>Registration helpers for the Selfwork Acquiring client.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAcquiringService"/> and <see cref="IWebhookVerifier"/> using
    /// configuration from <paramref name="configuration"/> under the
    /// <see cref="SelfworkOptions.Section"/> key.
    /// Includes an authenticated typed <see cref="System.Net.Http.HttpClient"/> with
    /// configurable retry and per-request timeout resilience policies.
    /// </summary>
    public static IServiceCollection AddSelfworkAcquiring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SelfworkOptions>()
            .Bind(configuration.GetSection(SelfworkOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<AuthHeaderHandler>();
        services.AddSingleton<IWebhookVerifier, WebhookVerifier>();

        services
            .AddHttpClient<IAcquiringService, AcquiringService>((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<SelfworkOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/'));
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddResilienceHandler("selfwork", (builder, context) =>
            {
                var opts = context.ServiceProvider.GetRequiredService<IOptions<SelfworkOptions>>().Value;

                if (opts.RetryCount > 0)
                {
                    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        MaxRetryAttempts = opts.RetryCount,
                        UseJitter = true,
                        ShouldHandle = args => args.Outcome switch
                        {
                            { Exception: HttpRequestException } => PredicateResult.True(),
                            { Result: { } r } when (int)r.StatusCode >= 500 => PredicateResult.True(),
                            _ => PredicateResult.False(),
                        },
                    });
                }

                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds),
                });
            });

        return services;
    }
}
