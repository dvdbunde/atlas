// --------------------------------------------------------------------------
// Azure RBAC Role Assignment
// Generic reusable module for assigning Azure RBAC roles.
// No knowledge of any specific Azure role — completely generic.
// Scope is provided as a parameter (the resource ID to assign the role at).
// Uses deterministic naming via guid() for idempotent deployments.
// --------------------------------------------------------------------------

@description('The Azure resource ID to scope the role assignment to')
param scope string

@description('The principal ID to assign the role to')
param principalId string

@description('The role definition ID (the GUID of the built-in or custom role)')
param roleDefinitionId string

@description('A short description for the role assignment name (e.g. "api-acrpull")')
param assignmentName string

// Deterministic role assignment name based on stable identifiers.
// Ensures idempotency across redeployments.
var roleAssignmentName = guid(scope, principalId, roleDefinitionId, assignmentName)

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: roleAssignmentName
  scope: resourceGroup(scope)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

output id          string = roleAssignment.id
output name        string = roleAssignment.name
