// --------------------------------------------------------------------------
// Azure Monitor Workbooks (O6 – Azure Workbooks & Grafana Dashboards)
//
// Provisions the ATLAS Operations Workbook as a first-class ARM resource so it
// is deployed idempotently with the rest of the infrastructure and survives
// environment rebuilds. The workbook definition (serialized JSON) lives in
// infra/telemetry/ and is loaded at deployment time.
//
// Workbooks are scoped to the resource group; queries inside the workbook
// target the Application Insights resource and Log Analytics workspace via
// workbook parameters.
//
// Authentication model: unlike Grafana (which queries via its managed
// identity), a Workbook executes its queries under the permissions of the
// user viewing it. Viewers therefore need their own read access to the
// Application Insights / Log Analytics data the workbook queries.
// --------------------------------------------------------------------------

@description('Deterministic unique name for the workbook resource (GUID format required by the provider)')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Serialized workbook definition (Notebook/1.0 JSON)')
param serializedData string

@description('Display name shown in Azure Monitor > Workbooks')
param displayName string

resource workbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: name
  location: location
  tags: tags
  kind: 'shared'
  properties: {
    displayName: displayName
    serializedData: serializedData
    category: 'workbook'
    version: '1.0'
  }
}

output id          string = workbook.id
// ARM resource name (GUID) — this is what bootstrap uses to locate the workbook.
output name        string = workbook.name
output displayName string = workbook.properties.displayName
