// --------------------------------------------------------------------------
// Azure Service Health Alert (O7 – Alerts & Operational Readiness)
//
// Activity-log alert scoped to the resource group that fires on Azure
// platform/service issues affecting ATLAS resources. Distinguishes Azure-side
// platform problems from application failures — the operator should not debug
// application code for a platform incident.
//
// Uses the ServiceHealth event category of the activity log alert pipeline.
// --------------------------------------------------------------------------

@description('Name of the service health alert rule')
param name string

@description('Resource tags')
param tags object

@description('Description shown in the alert and notifications')
param alertDescription string

@description('Resource ID of the Action Group notified when the alert fires')
param actionGroupId string

resource serviceHealthAlert 'Microsoft.Insights/activityLogAlerts@2020-10-01' = {
  name: name
  location: 'Global'
  tags: tags
  properties: {
    description: alertDescription
    enabled: true
    scopes: [
      subscription().id
    ]
    condition: {
      allOf: [
        {
          field: 'category'
          equals: 'ServiceHealth'
        }
        // incidentType must use OR semantics — a single event has exactly one
        // type, so these alternatives belong in anyOf (an allOf here would be
        // unsatisfiable and the alert could never fire).
        {
          anyOf: [
            {
              field: 'properties.incidentType'
              equals: 'Incident'
            }
            {
              field: 'properties.incidentType'
              equals: 'Maintenance'
            }
            {
              field: 'properties.incidentType'
              equals: 'Security'
            }
          ]
        }
      ]
    }
    actions: {
      actionGroups: [
        {
          actionGroupId: actionGroupId
        }
      ]
    }
  }
}

output id   string = serviceHealthAlert.id
output name string = serviceHealthAlert.name
