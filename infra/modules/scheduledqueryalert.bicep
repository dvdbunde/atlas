// --------------------------------------------------------------------------
// Azure Monitor Scheduled Query Alert (O7 – Alerts & Operational Readiness)
//
// Generic reusable module for log-based (KQL) alert rules evaluated against
// the ATLAS Application Insights / Log Analytics workspace. Used for:
//   - Exception spike detection (exceptions table)
//   - Email delivery failure rate (atlas.email.sends custom metric)
//   - Sustained command latency (atlas.command.duration custom metric)
//
// Aggregation model (non-obvious Azure Monitor behavior):
// The queries used with this module END IN summarize and return exactly ONE
// row containing the measured numeric value (count of exceptions, failure
// sum, p95 duration). For such queries, criteria.timeAggregation must NOT be
// 'Count' — Count counts returned ROWS (always 1 here), so row-count
// thresholds like "> 50" could never fire. Instead 'Maximum' is used: Azure
// Monitor applies the aggregation to the numeric column of the result over
// the evaluation window, so the threshold compares the WORST observed
// summarized value against the threshold. With one row per evaluation this
// is simply the summarized value itself.
//
// BCP081 note: az bicep build emits a warning that
// Microsoft.Insights/scheduledQueryRules@2022-08-01 has no Bicep type
// definitions. This is expected — Bicep cannot validate the resource
// properties at compile time; the resource type is intentionally still used.
// Runtime validation by Azure during deployment remains required.
//
// Idempotency: rules are named deterministically and updated in place.
// --------------------------------------------------------------------------

@description('Name of the scheduled query alert rule')
param name string

@description('Resource tags')
param tags object

@description('Description shown in the alert and notifications')
param alertDescription string

@description('Alert severity: 0 (critical) .. 4 (verbose)')
param severity int

@description('KQL query evaluated on schedule. Must return a single numeric column.')
param query string

@description('Full resource IDs to query (Log Analytics workspace and/or App Insights)')
param resourceIds array

@description('Evaluation frequency in ISO-8601 duration (e.g. PT15M)')
param evaluationFrequency string = 'PT15M'

@description('Look-back period of the query in ISO-8601 duration (e.g. PT1H)')
param windowSize string = 'PT1H'

@description('Comparison operator for the threshold')
param operator string = 'GreaterThan'

@description('Static threshold the query result is compared against')
param threshold int

@description('Number of consecutive violations required before firing')
param failureCount int = 2

@description('Resource ID of the Action Group notified when the alert fires')
param actionGroupId string

resource scheduledQueryAlert 'Microsoft.Insights/scheduledQueryRules@2022-08-01' = {
  name: name
  location: resourceGroup().location
  tags: tags
  properties: {
    displayName: name
    description: alertDescription
    severity: severity
    enabled: true
    scopes: resourceIds
    evaluationFrequency: evaluationFrequency
    windowSize: windowSize
    autoMitigate: true
    targetResourceTypes: []
    criteria: {
      query: query
      // 'Maximum' (not 'Count'): the query returns one summarized row; the
      // threshold must compare that numeric value, not the row count.
      timeAggregation: 'Maximum'
      operator: operator
      threshold: threshold
      failingPeriods: {
        numberOfEvaluationPeriods: failureCount
        minFailingPeriodsToAlert: failureCount
      }
    }
    actions: {
      actionGroups: [
        actionGroupId
      ]
    }
  }
}

output id   string = scheduledQueryAlert.id
output name string = scheduledQueryAlert.name
