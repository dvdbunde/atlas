//----------------------
// AtlasMetrics (O4 – Metrics & Telemetry)
// Central definition of all ATLAS metric instruments. Metric names and
// dimensions are defined here once so they remain stable, low-cardinality,
// and queryable in Azure Monitor / Grafana.
//
// Conventions:
// - Meter name: "ATLAS.Application" (same source as O3 tracing).
// - Names: dot-separated, stable, low-cardinality ("atlas.<area>.<name>").
// - Dimensions: only low-cardinality values (outcome, operation type).
//   NEVER use ApplicationId, UserId, DocumentId, email addresses, blob names,
//   or other high-cardinality/PII values as dimensions.
// - Counters increment exactly once per logical event at a clearly defined
//   boundary (see docs/architecture/azure-infrastructure.md).
//----------------------

using System.Diagnostics.Metrics;

namespace ATLAS.Application.Telemetry
{
    /// <summary>
    /// Central ATLAS metric instruments.
    /// </summary>
    public static class AtlasMetrics
    {
        /// <summary>Meter name — registered by host OpenTelemetry configuration.</summary>
        public const string MeterName = ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName;

        private static readonly Meter Meter = new(MeterName);

        // ------------------------------------------------------------------
        // Business metrics (emitted in application command handlers)
        // ------------------------------------------------------------------

        /// <summary>
        /// Count of permit-application lifecycle transitions.
        /// Dimension "transition": created | submitted | approved | rejected |
        /// info_requested | resubmitted (cardinality: 6, fixed).
        /// Emitted once per successful business transition in the corresponding
        /// command handler — never in infrastructure or event handlers.
        /// </summary>
        public static readonly Counter<long> ApplicationTransitions =
            Meter.CreateCounter<long>(
                "atlas.applications.transitions",
                unit: "{application}",
                description: "Permit application lifecycle transitions");

        // ------------------------------------------------------------------
        // Technical metrics
        // ------------------------------------------------------------------

        /// <summary>
        /// Email delivery outcomes.
        /// Dimension "outcome": success | failure (cardinality: 2, fixed).
        /// Emitted once per logical send attempt at the AcsEmailService boundary;
        /// LocalEmailService (dev sink) does not emit.
        /// </summary>
        public static readonly Counter<long> EmailSends =
            Meter.CreateCounter<long>(
                "atlas.email.sends",
                unit: "{email}",
                description: "Email delivery attempts by outcome");

        /// <summary>
        /// Command execution duration (milliseconds), recorded by TracingBehavior
        /// around every MediatR command (the Blazor-initiated operation boundary).
        /// Dimension "command": stable command type name (cardinality: bounded by
        /// the number of command types in the application, currently ~30).
        /// Duration percentiles are also derivable from O3 trace spans; this
        /// histogram makes them available as native metrics without log queries.
        /// </summary>
        public static readonly Histogram<double> CommandDuration =
            Meter.CreateHistogram<double>(
                "atlas.command.duration",
                unit: "ms",
                description: "MediatR command execution duration");

        /// <summary>
        /// Email delivery duration (milliseconds), recorded at the AcsEmailService
        /// boundary per send attempt. Dimension "outcome": success | failure
        /// (cardinality: 2, fixed).
        /// </summary>
        public static readonly Histogram<double> EmailDuration =
            Meter.CreateHistogram<double>(
                "atlas.email.duration",
                unit: "ms",
                description: "Email delivery duration by outcome");
    }
}