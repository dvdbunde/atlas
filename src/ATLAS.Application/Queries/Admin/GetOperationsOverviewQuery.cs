//----------------------
// GetOperationsOverviewQuery (O5 – Operations Portal)
// Aggregates current operational state for the Administration Operations page:
// system health (from the existing health-check infrastructure) and cumulative
// O4 metric counters. Read-only; no telemetry is persisted.
//----------------------

using ATLAS.Application.Telemetry;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ATLAS.Application.Queries.Admin
{
    public class GetOperationsOverviewQuery : IRequest<OperationsOverviewDto>
    {
    }

    /// <summary>Read model for the Administration Operations overview.</summary>
    public class OperationsOverviewDto
    {
        /// <summary>Overall health status: Healthy, Degraded, Unhealthy — or null when unavailable.</summary>
        public string? OverallHealth { get; init; }

        /// <summary>Per-component health entries. Empty when health data is unavailable.</summary>
        public IReadOnlyList<OperationsHealthEntryDto> HealthEntries { get; init; }
            = Array.Empty<OperationsHealthEntryDto>();

        /// <summary>True when health data could not be retrieved at all.</summary>
        public bool HealthUnavailable { get; init; }

        /// <summary>Cumulative application transitions since process start. Null when no metrics have been recorded.</summary>
        public IReadOnlyDictionary<string, long>? ApplicationTransitions { get; init; }

        /// <summary>Cumulative email sends by outcome since process start. Null when no metrics have been recorded.</summary>
        public IReadOnlyDictionary<string, long>? EmailSends { get; init; }

        /// <summary>When the snapshot was taken (UTC).</summary>
        public DateTime CapturedAtUtc { get; init; }
    }

    /// <summary>Health status of a single registered health check.</summary>
    public class OperationsHealthEntryDto
    {
        public string Name { get; init; } = string.Empty;

        /// <summary>Healthy | Degraded | Unhealthy</summary>
        public string Status { get; init; } = string.Empty;

        public string? Description { get; init; }
    }

    public class GetOperationsOverviewQueryHandler
        : IRequestHandler<GetOperationsOverviewQuery, OperationsOverviewDto>
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly IOperationsMetricsSnapshot _metricsSnapshot;
        private readonly ILogger<GetOperationsOverviewQueryHandler> _logger;

        public GetOperationsOverviewQueryHandler(
            HealthCheckService healthCheckService,
            IOperationsMetricsSnapshot metricsSnapshot,
            ILogger<GetOperationsOverviewQueryHandler> logger)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _metricsSnapshot = metricsSnapshot ?? throw new ArgumentNullException(nameof(metricsSnapshot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OperationsOverviewDto> Handle(
            GetOperationsOverviewQuery request,
            CancellationToken cancellationToken)
        {
            string? overall = null;
            var entries = new List<OperationsHealthEntryDto>();
            var healthUnavailable = false;

            try
            {
                // Runs all registered health checks (database, storage, key vault).
                var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

                overall = report.Status.ToString();
                entries = report.Entries
                    .Select(e => new OperationsHealthEntryDto
                    {
                        Name = e.Key,
                        Status = e.Value.Status.ToString(),
                        Description = e.Value.Description
                    })
                    .OrderBy(e => e.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Health retrieval failed — surface as explicitly unavailable,
                // never as "unhealthy" or zero.
                _logger.LogWarning(ex, "Unable to retrieve health check data for Operations overview");
                healthUnavailable = true;
            }

            // Metrics: null (not zero) when nothing has been recorded yet, so the
            // UI can distinguish "no activity" from "telemetry unavailable".
            IReadOnlyDictionary<string, long>? transitions =
                _metricsSnapshot.HasData ? _metricsSnapshot.ApplicationTransitions : null;
            IReadOnlyDictionary<string, long>? emailSends =
                _metricsSnapshot.HasData ? _metricsSnapshot.EmailSends : null;

            return new OperationsOverviewDto
            {
                OverallHealth = overall,
                HealthEntries = entries,
                HealthUnavailable = healthUnavailable,
                ApplicationTransitions = transitions,
                EmailSends = emailSends,
                CapturedAtUtc = DateTime.UtcNow
            };
        }
    }
}