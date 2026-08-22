# ATLAS Operational Runbook (Milestone 11 – O7)

Concise operator guide for responding to ATLAS Azure Monitor alerts. Azure
Monitor is the authoritative alerting platform; the ATLAS Operations Portal is
a curated overview; the Operations Workbook and Grafana dashboard are the
deeper investigation tools.

## Investigation flow

1. **Read the alert** — its description names the signal and likely cause.
2. **ATLAS Operations Portal** — check overall health and current metrics.
3. **Application Insights / Azure Monitor** — inspect the raw signal named in
   the alert description.
4. **Operations Workbook / Grafana dashboard** — trend analysis over time,
   breakdown by command type / outcome / problemId.
5. **Inspect the relevant Azure dependency** — SQL, Storage, Key Vault, ACS
   (see per-alert guidance below).
6. **Confirm recovery** — alerts auto-mitigate when the condition clears;
   verify the signal returned to normal on the Grafana dashboard before
   closing out.

## Alerts

### atlas-{env}-api-availability / atlas-{env}-blazor-availability (Sev 1)

- **Meaning**: App Service health check (`/health/ready`) failing for 15+
  minutes on the API or Blazor app. The application is not serving healthy
  responses.
- **Likely causes**: dependency outage (SQL paused, Key Vault inaccessible,
  blob container missing), crashed container, bad deployment.
- **Investigate**: Portal → health status; Application Insights → availability
  and exceptions; `az webapp log tail` for startup failures. Check whether the
  SQL database auto-paused (serverless tier) — first request resumes it.
- **First checks**: Is one app or both affected? Both → shared dependency
  (SQL/Key Vault). One → app-specific deployment/container issue.
- **Recovery**: `/health/ready` returns healthy; HealthCheckStatus metric = 1.

### atlas-{env}-exception-spike (Sev 2)

- **Meaning**: Sustained elevated exception volume (>50/hour, two consecutive
  evaluations).
- **Likely causes**: regression after deployment, dependency failure manifesting
  as exceptions, invalid input handling.
- **Investigate**: Application Insights → Exceptions by problemId; Workbook →
  "Exceptions over time" panel; correlate with recent deployments.
- **Recovery**: exception rate back under threshold.

### atlas-{env}-email-failures (Sev 2)

- **Meaning**: More than 4 failed email sends (`atlas.email.sends`, outcome=failure)
  within an hour, sustained across two evaluations. A single failure does not
  fire this alert.
- **Likely causes**: ACS outage/quota, sender address misconfiguration,
  invalid recipient data.
- **Investigate**: ACS RequestLogs in Log Analytics; Email panel on the Grafana
  dashboard; verify ACS sender configuration in bootstrap output.
- **Recovery**: outcome=success counter resuming; failures stop accumulating.

### atlas-{env}-command-latency (Sev 3)

- **Meaning**: p95 of `atlas.command.duration` above 10 seconds for an hour —
  sustained performance degradation.
- **Likely causes**: slow SQL query, storage throttling, downstream dependency
  slowness, resource contention on the App Service plan.
- **Investigate**: Grafana dashboard → Command duration p95 panel to identify
  which command type; Application Insights → dependencies for that operation;
  SQL insights diagnostic logs.
- **Recovery**: p95 back below threshold.

### atlas-{env}-service-health (Sev 2)

- **Meaning**: Azure platform incident/maintenance/security advisory affecting
  resources in the ATLAS resource group. **This is not an application bug — do
  not debug application code first.**
- **Investigate**: Azure Service Health blade for scope, updates, and ETA.
- **Recovery**: Azure marks the incident resolved; confirm application metrics
  normalized afterwards.

## Common scenarios

| Scenario | Start here |
| --- | --- |
| Application errors | Exception-spike alert → App Insights exceptions by problemId |
| Email delivery failures | Email-failures alert → ACS RequestLogs + Grafana email panel |
| Slow commands/requests | Command-latency alert → Grafana p95 panel → dependency traces |
| Failed dependency | Availability alert + Portal health tiles → specific dependency health check |
| Application unavailable | Sev 1 availability alert → both apps? shared dependency vs. single-app issue |
| Azure platform problem | Service-health alert → Azure Service Health blade |

## Notification routing

All alerts route to a single Action Group (`atlas-{env}-ops-ag`) with an email
receiver supplied as a deployment parameter (never hard-coded in source
control). To change recipients, update the parameter and redeploy — no manual
portal edits required.

## Boundaries

- The business Audit Log is permanent business history and is never used as an
  alert source.
- The Operations Portal does not manage alerts (no acknowledge/suppress).
- O7 is detection only: no automatic restart, scaling, or remediation exists.
