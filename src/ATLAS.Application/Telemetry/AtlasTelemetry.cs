//----------------------
// AtlasTelemetry (O3 – Correlation & Distributed Tracing)
// Single authoritative definition of the ATLAS ActivitySource name.
// Referenced by TracingBehavior (Activity creation) and host OpenTelemetry
// registrations (AddSource) so the name can never drift between them.
//----------------------

namespace ATLAS.Application.Telemetry
{
    /// <summary>
    /// Central ATLAS telemetry constants.
    /// </summary>
    public static class AtlasTelemetry
    {
        /// <summary>
        /// The ActivitySource name for ATLAS application operations.
        /// Registered via AddSource in host OpenTelemetry configuration and used
        /// by TracingBehavior when creating command Activities.
        /// </summary>
        public const string ActivitySourceName = "ATLAS.Application";
    }
}