using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kongroo.Catalog.Infrastructure;

/// <summary>Reports whether Game Search can reach Elasticsearch.</summary>
public sealed class ElasticsearchHealthCheck(ElasticsearchClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var response = await client.PingAsync(cancellationToken);

        return response.IsValidResponse
            ? HealthCheckResult.Healthy()
            : new HealthCheckResult(
                context.Registration.FailureStatus,
                "Elasticsearch did not answer the ping.",
                response.ApiCallDetails.OriginalException
            );
    }
}
