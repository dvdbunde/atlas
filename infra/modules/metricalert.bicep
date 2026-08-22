// --------------------------------------------------------------------------
// Azure Monitor Metric Alert (O7 – Alerts & Operational Readiness)
//
// Generic reusable module for single-resource metric alerts (static
// threshold). Used for the App Service health-check availability alert.
//
// Idempotency: alert rules are named deterministically and updated in place
// on re-deployment.
// --------------------------------------------------------------------------

@description('Name of the metric alert rule')
param name string

@description('Resource tags')
param tags object

@description('Full resource ID of the target resource to monitor')
param targetResourceId string

@description('Alert description shown in the alert and notifications')
param alertDescription string

@description('Alert severity: 0 (critical) .. 4 (verbose)')
param severity int

@description('Metric namespace of the signal (e.g. Microsoft.Web/sites)')
param metricNamespace string

@description('Metric name of the signal')
param metricName string

@description('Aggregation applied over the evaluation window')
param aggregation string = 'Average'

@description('Comparison operator for the threshold')
param operator string = 'LessThan'

@description('Static threshold value')
param threshold int

@description('Evaluation frequency in ISO-8601 duration (e.g. PT5M)')
param evaluationFrequency string = 'PT5M'

@description('Look-back window in ISO-8601 duration (e.g. PT15M)')
param windowSize string = 'PT15M'

@description('Resource ID of the Action Group notified when the alert fires')
param actionGroupId string

resource metricAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    description: alertDescription
    severity: severity
    enabled: true
    scopes: [
      targetResourceId
    ]
    evaluationFrequency: evaluationFrequency
    windowSize: windowSize
    autoMitigate: true
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'criterion1'
          criterionType: 'StaticThresholdCriterion'
          metricNamespace: metricNamespace
          metricName: metricName
          dimensions: []
          operator: operator
          threshold: threshold
          timeAggregation: aggregation
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroupId
      }
    ]
  }
}

output id   string = metricAlert.id
output name string = metricAlert.name
