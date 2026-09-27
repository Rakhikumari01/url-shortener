using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UrlShortener.Data;

namespace UrlShortener.HealthChecks
{
    /// <summary>
    /// Readiness probe for Postgres. Deliberately not part of liveness: a
    /// database outage should take the instance out of the load balancer, not
    /// get the container restarted (a restart cannot fix someone else's DB).
    /// </summary>
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

        private readonly AppDbContext _db;

        public DatabaseHealthCheck(AppDbContext db) => _db = db;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken = default)
        {
            // EnableRetryOnFailure would otherwise keep this probe hanging for
            // ~25s against a dead database. A probe must answer fast or it is
            // useless to the orchestrator.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ProbeTimeout);

            try
            {
                await _db.Database.ExecuteSqlRawAsync("SELECT 1", timeout.Token)
                    .ConfigureAwait(false);
                return HealthCheckResult.Healthy("Postgres reachable.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return HealthCheckResult.Unhealthy($"Postgres did not respond within {ProbeTimeout.TotalSeconds:0.#}s.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Postgres is not reachable.", ex);
            }
        }
    }
}
