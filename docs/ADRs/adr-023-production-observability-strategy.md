---
title: "ADR-023: Production Observability Strategy"
status: "Accepted"
date: "2026-07-28"
authors: "Engineering Team"
tags: ["architecture", "observability", "monitoring", "logging", "azure", "milestone-9"]
supersedes: ""
superseded_by: ""
---

# ADR-023: Production Observability Strategy

## Status

### Accepted

## Context

Milestone 9 introduces production deployment. In production, developers cannot attach a debugger, inspect local variables, or step through code. Diagnosing production issues must rely entirely on telemetry emitted by the running application.

Earlier milestones implemented logging through `ILogger<T>` and MediatR pipeline behaviors, but observability was treated as an implementation concern rather than an architectural one. Individual handlers and services logged information at their discretion, with no consistent approach to correlation, structured data, or health reporting. The result was a logging system that could answer "what happened" but not "why did it happen" or "is the system healthy right now."

Production observability requires three capabilities that debugging does not:

1. **Health assessment**: Does the application respond correctly to requests? Are its dependencies (database, blob storage, authentication) available?
2. **Root cause analysis**: When a request fails, can the operator trace the failure from the HTTP request through the MediatR pipeline, through database calls, to the root cause?
3. **Proactive detection**: Can the operations team identify degrading performance, increasing error rates, or resource exhaustion before users report issues?

Observability is treated as an architectural concern because it affects every layer of the application — from the API controllers through the MediatR pipeline to the infrastructure services — and must be designed into the application rather than added after deployment.

## Decision

Production deployments shall provide sufficient telemetry to diagnose application health without requiring interactive debugging. The observability strategy includes the following capabilities:

### Health Checks

- ASP.NET Core Health Checks expose the application's ability to serve requests. Endpoints report overall health and individual dependency health (database connectivity, blob storage reachability, Key Vault accessibility).
- Health checks are used by Azure App Service to drive load balancer routing and by monitoring alerts to trigger incident response.

### Structured Logging

- All log messages use structured templates with named placeholders, not string interpolation. Structured properties enable querying, filtering, and aggregation in the telemetry backend.
- Log levels are applied consistently: `Error` for failures, `Warning` for degraded conditions, `Information` for normal operation, `Debug` and `Trace` for diagnostic detail.

### Distributed Correlation

- Every incoming HTTP request receives a correlation ID that propagates through the MediatR pipeline, through database calls, and through outgoing HTTP requests. The correlation ID links all telemetry from a single request into a unified trace.
- The ASP.NET Core `Activity` API (OpenTelemetry) provides the correlation infrastructure without coupling to a specific telemetry vendor.

### Application Insights

- Application Insights is the telemetry backend for production. It ingests logs, metrics, and distributed traces into a single queryable workspace.
- Application Insights SDK integration is configured at the application level, not sprinkled across individual handlers. The MediatR pipeline behaviors and ASP.NET Core middleware are the instrumentation boundaries.

### Exception Telemetry

- Unhandled exceptions are captured automatically by ASP.NET Core middleware and reported to Application Insights. Handled exceptions (expected failures, validation errors) are logged at the appropriate level without triggering alerts.
- Exception telemetry includes the full call stack, request context, and correlation ID.

## Consequences

### Positive

- **Production debugging without a debugger**: Operators can diagnose the root cause of failures using telemetry queries alone. The correlation ID connects a user's HTTP request to the database queries and external calls it triggered.
- **Proactive incident detection**: Health checks and metric alerts detect degradation before users are affected. Error rate thresholds trigger automated responses.
- **Consistent instrumentation**: Developers add observability through pipeline behaviors and middleware, not through ad-hoc logging in individual handlers. This reduces the cognitive load of writing observable code.
- **Vendor flexibility**: The OpenTelemetry-based Activity API keeps the application decoupled from Application Insights. If the telemetry backend changes in the future, the infrastructure layer changes but the application code does not.
- **Performance baseline**: Telemetry provides a historical performance baseline. Regressions (increased database latency, decreased throughput) are visible immediately after deployment.

### Negative

- **Telemetry cost**: Application Insights charges based on data ingestion volume. High-traffic scenarios may generate significant telemetry costs. Sampling strategies and ingestion limits must be configured to balance observability with cost.
- **Noise**: Without careful configuration, telemetry can generate noise that obscures real issues. Alert fatigue is a risk if thresholds are set too aggressively.
- **Operational overhead**: Maintaining the observability stack (Application Insights configuration, alert rules, dashboard creation, sampling tuning) requires ongoing attention. This is a new operational responsibility for the team.
- **False negatives**: A health check that reports healthy when the application is actually degraded is worse than no health check. Health check implementations must test real dependency availability, not just process liveness.

## Alternatives Considered

### Logging Only

- **Description**: Relying solely on application logs written to files or stdout, with no distributed tracing, health checks, or metrics infrastructure.
- **Rejection Reason**: Logs alone cannot answer "is the application healthy" or "what was the full execution path of a failing request." Without correlation IDs, logs from different services and components cannot be linked. Healthy/unhealthy states require external monitoring.

### Custom Diagnostics

- **Description**: Building a custom diagnostics dashboard that exposes application health, metrics, and tracing through a custom API endpoint.
- **Rejection Reason**: Building a custom observability system is a significant engineering effort that duplicates the capabilities of Application Insights. The team would need to implement data storage, querying, alerting, and visualization — all of which are provided out of the box by Application Insights.

### Third-Party Monitoring (Datadog, New Relic)

- **Description**: Using a third-party observability platform instead of Application Insights.
- **Rejection Reason**: Third-party monitoring tools add cost, another vendor relationship, and additional integration effort. Application Insights is included in the Azure ecosystem, integrates natively with App Service, Azure SQL Database, and other managed services, and provides the telemetry capabilities ATLAS requires. Third-party tools may be evaluated if future requirements outgrow Application Insights.

## References

- ADR-001: Clean Architecture (observability cross-cutting concerns are implemented as pipeline behaviors and middleware)
- ADR-005: Blazor Server (SignalR connection health monitoring integrates with the health check infrastructure)
- ADR-019: Azure App Service as the Primary Production Hosting Platform (App Service integrates with Application Insights for runtime monitoring)
- ADR-022: Managed Azure Services over Self-Hosted Infrastructure (Application Insights is a managed observability service)
