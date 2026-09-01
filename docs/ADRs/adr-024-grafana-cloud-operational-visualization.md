---
title: "ADR-024: Grafana Cloud for Operational Visualization"
status: "Implemented"
date: "2026-09-01"
authors: "Engineering Team"
tags: ["architecture", "observability", "grafana", "azure", "milestone-11"]
supersedes: ""
superseded_by: ""
---

# ADR-024: Grafana Cloud for Operational Visualization

## Status

### Implemented

## Context

M11 requires an operational visualization surface for Azure Monitor telemetry. Azure Monitor, Application Insights and Log Analytics remain the telemetry and alerting foundation, but a dedicated dashboard experience is useful for day-to-day operational overview and trend analysis.

An earlier M11 implementation explored Azure Managed Grafana. The final operational direction is Grafana Cloud instead. The ATLAS application does not need to host or operate a Grafana service in Azure.

## Decision

ATLAS uses **Grafana Cloud** as the external operational dashboard platform.

For each ATLAS environment, a Microsoft Entra application is named:

```text
atlas-grafana-dev
atlas-grafana-test
atlas-grafana-prod
```

The post-deployment bootstrap resolves the application's service principal and assigns the built-in Azure **Reader** role at the environment's ATLAS resource-group scope:

```text
atlas-grafana-{env}
        ↓
service principal
        ↓
Reader
        ↓
atlas-{env}-rg
```

The bootstrap is environment-parameterized and does not contain a development-only application name.

## Consequences

### Positive

- No Azure Managed Grafana hosting cost or resource lifecycle to manage.
- Grafana dashboards remain available as a dedicated visualization surface.
- Azure-side authorization uses a normal Entra application/service principal and Azure RBAC.
- The same bootstrap logic works for dev, test and prod.
- Dashboard definitions can remain version-controlled in `infra/telemetry/`.

### Negative

- Grafana Cloud is an external service and therefore introduces a separate service boundary.
- The Azure resource-group Reader role is broader than a narrowly scoped per-resource assignment, but keeps the integration simple and environment-consistent.
- Grafana Cloud availability is separate from Azure Monitor availability; Azure Monitor remains the authoritative telemetry/alerting platform.

## Boundaries

Grafana Cloud is **not** the telemetry store of record and is **not** the alerting authority. Azure Monitor/Application Insights/Log Analytics remain authoritative for technical telemetry and alerts.

The business Audit Log remains a separate source of permanent business history.

## Implementation

The environment-specific access is configured by:

```text
infra/bootstrap-revised.ps1
```

The bootstrap resolves `atlas-grafana-$Environment`, obtains its service principal object ID and creates/verifies the Reader assignment at the supplied `$ResourceGroup` scope.

The source-controlled dashboard definition is:

```text
infra/telemetry/atlas-operations.grafana-dashboard.json
```

## Migration note

Any remaining Azure Managed Grafana Bicep/modules or deployment parameters from the earlier M11 implementation are legacy code and should be removed/reconciled separately. They are not part of the final Grafana Cloud architecture.
