// --------------------------------------------------------------------------
// Azure Monitor Action Group (O7 – Alerts & Operational Readiness)
//
// Single notification route for all ATLAS alert rules. Keeps routing simple:
// one action group, email notification, severity-agnostic.
//
// Notification recipients are NOT hard-coded in source control. The email
// address is supplied as a deployment parameter (see main.parameters.*.json,
// which lives outside the repo default or is overridden at deploy time).
// Additional channels (SMS, webhook, ITSM) can be added here later without
// changing any alert rule.
// --------------------------------------------------------------------------

@description('Name of the Action Group')
param name string

@description('Resource tags')
param tags object

@description('Short group name shown in notifications (max 12 chars)')
param groupShortName string = 'atlas-ops'

@description('Email address for alert notifications. Supplied per environment; never hard-coded in source control.')
param emailReceiver string

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    groupShortName: groupShortName
    enabled: true
    emailReceivers: [
      {
        name: 'ops-email'
        emailAddress: emailReceiver
        useCommonAlertSchema: true
      }
    ]
    smsReceivers: []
    webhookReceivers: []
    itsmReceivers: []
    azureAppPushReceivers: []
    automationRunbookReceivers: []
    voiceReceivers: []
    logicAppReceivers: []
    azureFunctionReceivers: []
    eventHubReceivers: []
  }
}

output id   string = actionGroup.id
output name string = actionGroup.name
