# ATLAS Operational Runbook — Milestone 11

Practical operator guide for troubleshooting the ATLAS application in a live Azure environment.

This runbook is based on the M11 observability/operations work and the live Azure verification performed against the deployed ATLAS environment.

## 1. Observability map

| Signal | Where to inspect | What it tells you |
| --- | --- | --- |
| Application health | App Service health / `/health/ready` | Whether API/Blazor instances are healthy |
| Application exceptions | Application Insights → Logs → `exceptions` | Application errors and `problemId` |
| Application requests | Application Insights → Logs → `requests` | Incoming HTTP operations and duration |
| Dependencies | Application Insights → Logs → `dependencies` | SQL/HTTP/Storage/etc. calls, duration and success |
| Custom application metrics | Application Insights → Logs → `customMetrics` | ATLAS transitions, commands and email telemetry |
| Command execution | `dependencies` / command-filtered query | Command duration and success |
| Email delivery | `customMetrics` + ACS logs | Send outcome and delivery timing |
| Azure platform diagnostics | Log Analytics → `AzureDiagnostics` | Platform/resource diagnostic activity |
| Alerts | Azure Monitor → Alerts | Detection and notification state |
| Alert notification | Action Group | Whether an alert notification was actually delivered |

The Operations Workbook is **not** part of the live operational path. It was removed from the deployment after repeated Azure Workbook resource-parameter issues. Grafana is likewise not currently part of the deployed monitoring path.

Azure Monitor/Application Insights and Log Analytics are the authoritative operational data sources.

---

## 2. Standard investigation flow

When an alert or operational problem is reported:

1. **Identify the affected application**
   - API only
   - Blazor only
   - Both
2. **Check application health**
   - App Service health
   - `/health/ready`
3. **Check Application Insights**
   - Exceptions
   - Requests
   - Dependencies
   - Custom metrics
4. **Check the relevant Azure dependency**
   - SQL
   - Storage/Blob
   - Key Vault
   - Azure Communication Services (ACS)
5. **Check Azure Monitor Alerts**
   - Fired/Resolved state
   - Condition
   - Evaluation time
   - Action Group notification
6. **Confirm recovery**
   - Health returns to healthy
   - Failed operations stop
   - Relevant metric returns to normal
   - Alert resolves

---

## 3. Application health and availability

### What availability means

ATLAS availability alerts are based on the application readiness endpoint:

`/health/ready`

A failure indicates that the application is not currently considered ready to serve traffic.

### First question: one app or both?

- **Both API and Blazor affected:** investigate shared infrastructure first:
  - SQL
  - Key Vault
  - Storage
- **Only one affected:** investigate that App Service/container and its recent deployment/configuration.

### SQL serverless consideration

The ATLAS development SQL database uses a serverless tier. A paused database can make the first health/application request slow or fail while the database resumes.

Do not immediately interpret a transient first-request failure as a permanent SQL outage.

---

## 4. Application Insights — exceptions

Open:

>**Application Insights → Logs**

Run:

```kusto
exceptions
| where timestamp > ago(1h)
| project
    timestamp,
    type,
    outerMessage,
    innermostMessage,
    operation_name,
    operation_Id,
    problemId
| order by timestamp desc
```

### What to look for

- Repeated `problemId`
- Same exception appearing across many requests
- Correlation with a recent deployment
- Dependency-related exceptions
- A sudden increase in volume

For a broader exception count over time:

```kusto
exceptions
| where timestamp > ago(1h)
| summarize Count = count() by problemId, bin(timestamp, 5m)
| order by timestamp asc
```

---

## 5. Application Insights — ATLAS custom metrics

The live deployment was verified to emit these ATLAS metrics:

- `atlas.applications.transitions`
- `atlas.command.duration`
- `atlas.email.sends`
- `atlas.email.duration`

The verified custom dimensions include:

- `transition`
- `command`
- `outcome`

Run:

```kusto
customMetrics
| where timestamp > ago(1h)
| where name startswith "atlas."
| project
    timestamp,
    name,
    value,
    valueCount,
    customDimensions
| order by timestamp desc
```

### Expected ATLAS command names observed during live testing

- `CreateDraftApplicationCommand`
- `SubmitDraftCommand`
- `ApproveDraftCommand`
- `AssignApplicationToMeCommand`
- `UploadDocumentCommand`

---

## 6. Application transitions

Use:

```kusto
customMetrics
| where timestamp > ago(1h)
| where name == "atlas.applications.transitions"
| extend Transition = tostring(customDimensions.transition)
| summarize Count = sum(todouble(value)) by Transition
| order by Count desc
```

During the live M11 scenario, the application lifecycle was successfully exercised:

- Created: `1`
- Submitted: `1`
- Approved: `1`

This confirms the transition telemetry was being emitted by the deployed application.

---

## 7. Command duration and performance

The live deployment was verified to emit `atlas.command.duration`.

Basic command performance query:

```kusto
customMetrics
| where timestamp > ago(1h)
| where name == "atlas.command.duration"
| extend command = tostring(customDimensions.command)
| summarize
    Count = sum(todouble(value)),
    p50 = percentile(todouble(value), 50),
    p95 = percentile(todouble(value), 95),
    p99 = percentile(todouble(value), 99),
    MaxDuration = max(todouble(value))
    by command
| order by p95 desc
```

### Command span verification

Command executions were also verified in Application Insights `dependencies`.

Use:

```kusto
union requests, dependencies
| where timestamp > ago(1h)
| where name in (
    "CreateDraftApplicationCommand",
    "SubmitDraftCommand",
    "ApproveDraftApplicationCommand",
    "AssignApplicationToMeCommand"
)
| project
    timestamp,
    itemType,
    name,
    duration,
    success,
    resultCode,
    operation_Name,
    operation_Id,
    customDimensions
| order by timestamp desc
```

Live verification observed successful command spans including:

- `AssignApplicationToMeCommand` — 29 ms
- `SubmitDraftCommand` — 4,439 ms
- `ApproveDraftCommand` — 349 ms

The command-latency alert is based on the p95 duration of `atlas.command.duration`.

---

## 8. Dependency investigation

Use this query to see recent dependency activity:

```kusto
dependencies
| where timestamp > ago(1h)
| project
    timestamp,
    name,
    type,
    target,
    duration,
    success,
    resultCode,
    operation_Name,
    operation_Id
| order by timestamp desc
```

To find failures:

```kusto
dependencies
| where timestamp > ago(1h)
| where success == false
| project
    timestamp,
    name,
    type,
    target,
    duration,
    resultCode,
    operation_Name,
    operation_Id
| order by timestamp desc
```

To summarize failures:

```kusto
dependencies
| where timestamp > ago(1h)
| summarize
    Count = count(),
    Failed = countif(success == false),
    AvgDurationMs = avg(duration),
    MaxDurationMs = max(duration)
    by name, type, target
| order by Failed desc, Count desc
```

### Dependency interpretation

| Observation | Likely direction |
| --- | --- |
| SQL dependency failures | SQL/database availability or query problem |
| SQL duration suddenly high | Database resume, slow query, contention |
| Storage dependency failures | Blob/storage configuration or availability |
| Key Vault-related failures | Identity/access/configuration |
| HTTP dependency failures | Downstream service |
| Dependencies healthy but command slow | Application processing/resource contention |

---

## 9. Email telemetry

The live deployment was verified to emit:

- `atlas.email.sends`
- `atlas.email.duration`

with the `outcome` dimension.

Query send outcomes:

```kusto
customMetrics
| where timestamp > ago(1h)
| where name == "atlas.email.sends"
| extend outcome = tostring(customDimensions.outcome)
| summarize Total = sum(todouble(value)) by outcome
| order by Total desc
```

Query email duration:

```kusto
customMetrics
| where timestamp > ago(1h)
| where name == "atlas.email.duration"
| extend outcome = tostring(customDimensions.outcome)
| summarize
    p50 = percentile(todouble(value), 50),
    p95 = percentile(todouble(value), 95),
    Samples = count()
    by outcome
```

### Live M11 verification

The end-to-end application scenario generated email telemetry. At the time of the live test the observed state included:

- Email successes: `0`
- Email failures: `2`
- Observed email durations: `199 ms` and `3916 ms`

This was useful because it demonstrated that email outcomes and durations were being recorded even when the send operation failed.

For an email failure, investigate Azure Communication Services (ACS), sender configuration, recipient data, quotas, and ACS request logs.

---

## 10. Traces

Command-specific trace queries were tested during M11 verification.

Example:

```kusto
traces
| where timestamp > ago(1h)
| where customDimensions["command"] != ""
| project
    timestamp,
    message,
    severityLevel,
    customDimensions
| order by timestamp desc
```

If no command traces are returned, do not assume the application has no command activity. The live verification showed that command execution can be observed through `dependencies` and `customMetrics`.

Use the signal that actually contains the required information.

---

## 11. Azure resource diagnostics / Log Analytics

The ATLAS Log Analytics workspace was verified with Azure CLI.

To list the workspace:

```powershell
az monitor log-analytics workspace list `
  --resource-group atlas-dev-rg `
  --query "[].id" `
  -o tsv
```

The verified development workspace was:

`atlas-dev-logs`

For diagnostic records:

```kusto
AzureDiagnostics
| where TimeGenerated > ago(1h)
| summarize Events = count() by Category, ResourceProvider
| order by Events desc
```

Use this when the problem appears to be at the Azure resource/platform level rather than inside application code.

---

## 12. Alert verification

The M11 alerting setup includes the following alert classes:

### Availability — Sev 1

`atlas-{env}-api-availability`

`atlas-{env}-blazor-availability`

Investigate:

1. `/health/ready`
2. App Service health
3. Application Insights exceptions
4. Application Insights dependencies
5. Shared dependencies if both applications fail

### Exception spike — Sev 2

`atlas-{env}-exception-spike`

Condition:

- More than 50 exceptions/hour
- Sustained across two evaluations

Investigate `exceptions` grouped by `problemId`.

### Email failures — Sev 2

`atlas-{env}-email-failures`

Condition:

- More than 4 failed email sends/hour
- Sustained across two evaluations

Investigate:

- `atlas.email.sends`
- ACS RequestLogs
- ACS sender configuration
- Recipient data

A single failed email does not fire this alert.

### Command latency — Sev 3

`atlas-{env}-command-latency`

Condition:

- p95 of `atlas.command.duration`
- Above 10 seconds
- Sustained for an hour

Investigate:

1. Which command has the highest p95
2. Its Application Insights dependency spans
3. SQL/storage/downstream dependency duration
4. App Service resource contention

### Azure Service Health — Sev 2

`atlas-{env}-service-health`

This indicates an Azure platform incident, maintenance event, or security advisory affecting the relevant scope.

Start with **Azure Service Health**, not application debugging.

---

## 13. Alert notification routing

All ATLAS alerts use the environment-specific Action Group:

`atlas-{env}-ops-ag`

The email receiver is supplied as a deployment parameter and is not hard-coded in source control.

To change the notification recipient, change the deployment parameter and redeploy the infrastructure.

Do not manually edit the production Action Group as the normal configuration path.

---

## 14. How to verify an alert end-to-end

For an alert test, verify all of these separately:

### 1. Signal exists

Run the relevant Application Insights/Log Analytics query and confirm the metric/event exists.

### 2. Alert condition is reached

Confirm the Azure Monitor alert changes to **Fired**.

### 3. Notification is generated

Check the alert's Action Group execution/history.

### 4. Email is received

Confirm the configured notification receiver actually receives the alert.

### 5. Recovery occurs

Return the application/metric to normal and confirm the alert changes back to **Resolved**.

The M11 live testing successfully demonstrated the command-latency alert firing and the notification email being received.

---

## 15. Quick troubleshooting matrix

| Problem | First query/check | Then inspect |
| --- | --- | --- |
| API unavailable | `/health/ready` | Exceptions → dependencies → SQL/Key Vault/Storage |
| Blazor unavailable | `/health/ready` | Exceptions → dependencies |
| Both apps unavailable | Health of both apps | SQL / Key Vault / Storage |
| Exceptions increasing | `exceptions` by `problemId` | Recent deployment + dependency failures |
| Commands slow | `atlas.command.duration` p95 | Command dependency spans |
| SQL slow | `dependencies` filtered to SQL | Database state/query performance |
| Email failures | `atlas.email.sends` by outcome | ACS RequestLogs + sender config |
| Email slow | `atlas.email.duration` | ACS/downstream dependency |
| Azure resource problem | `AzureDiagnostics` | Azure Service Health |
| Alert fired but no email | Alert + Action Group history | Receiver/configuration |
| No telemetry visible | Check App Insights resource/time range | Application deployment/configuration |

---

# 16. Useful live-query set

These are the core queries to keep ready during an incident.

## All ATLAS telemetry

```kusto
customMetrics
| where timestamp > ago(1h)
| where name startswith "atlas."
| project timestamp, name, value, customDimensions
| order by timestamp desc
```

## Exceptions

```kusto
exceptions
| where timestamp > ago(1h)
| project timestamp, type, outerMessage, innermostMessage, problemId, operation_name, operation_Id
| order by timestamp desc
```

## Dependency failures

```kusto
dependencies
| where timestamp > ago(1h)
| where success == false
| project timestamp, name, type, target, duration, resultCode, operation_Name, operation_Id
| order by timestamp desc
```

## Command latency

```kusto
customMetrics
| where timestamp > ago(1h)
| where name == "atlas.command.duration"
| extend command = tostring(customDimensions.command)
| summarize
    p50 = percentile(todouble(value), 50),
    p95 = percentile(todouble(value), 95),
    p99 = percentile(todouble(value), 99),
    Samples = count()
    by command
| order by p95 desc
```

## Email outcomes

```kusto
customMetrics
| where timestamp > ago(1h)
| where name == "atlas.email.sends"
| extend outcome = tostring(customDimensions.outcome)
| summarize Total = sum(todouble(value)) by outcome
| order by Total desc
```

## Azure diagnostics

```kusto
AzureDiagnostics
| where TimeGenerated > ago(1h)
| summarize Events = count() by Category, ResourceProvider
| order by Events desc
```

---

## 17. Operational boundaries

- **Application Insights / Azure Monitor** is the authoritative source for application telemetry and alerting.
- The **business Audit Log** is permanent business history and is not an alert source.
- Do not use business audit records as a substitute for operational telemetry.
- The ATLAS Operations Portal is a convenience/overview surface; investigate raw telemetry when diagnosing incidents.
- The Operations Portal does not manage or suppress alerts.
- Alerting is detection/notification only.
- There is no automatic restart, scaling, or incident remediation performed by the M11 alerting system.
- The Azure Workbook was abandoned and is not required for live troubleshooting.
- Managed Grafana is not deployed and is not required for the current operational path.

---

## 18. Live M11 verification baseline

The following was confirmed against the live deployment during M11 verification:

- ATLAS application lifecycle telemetry was emitted.
- `atlas.applications.transitions` was observed.
- `atlas.command.duration` was observed.
- `atlas.email.sends` was observed.
- `atlas.email.duration` was observed.
- Command dimensions were observed.
- Email outcome dimensions were observed.
- Application Insights `dependencies` contained command execution spans.
- Application Insights exceptions were queryable.
- Dependency records were queryable.
- Log Analytics workspace access was verified.
- The live application scenario successfully exercised create → submit → approve.
- The command-latency Azure Monitor alert was triggered successfully.
- The alert notification email was received successfully.
- The Azure Monitor alerting path therefore has been tested end-to-end in the live environment.

When a new deployment changes telemetry names, dimensions, alert thresholds, or resource wiring, repeat the relevant live verification rather than assuming the existing baseline still applies.
