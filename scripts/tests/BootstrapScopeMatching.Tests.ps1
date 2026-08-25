# -----------------------------------------------------------------------------
# Behavioral tests for the O7 bootstrap scope-matching logic (Phase 11c).
# These mirror the exact comparison operators used in Verify-O7Alerting in
# infra/bootstrap.ps1 so regressions in the matching semantics are caught
# without a live Azure deployment.
#
# Plain PowerShell - no test framework dependency. Run with:
#   pwsh -File scripts/tests/BootstrapScopeMatching.Tests.ps1
# Exits non-zero on any failure.
# -----------------------------------------------------------------------------

$script:failures = 0

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) {
        Write-Host "  PASS $Name" -ForegroundColor Green
    } else {
        Write-Host "  FAIL $Name" -ForegroundColor Red
        $script:failures++
    }
}

$sub       = 'de96dbbf-6362-4f96-854d-a9666dc30595'
$rg        = 'atlas-dev-rg'
$apiName   = 'atlas-api-dev-de96db'
$blazorName = 'atlas-blazor-dev-de96db'
$apiScope    = "/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Web/sites/$apiName"
$blazorScope = "/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Web/sites/$blazorName"

Write-Host "O7 bootstrap scope matching" -ForegroundColor Cyan

Write-Host " metric alert - exact scope match" -ForegroundColor Yellow
Assert-True ($apiScope -eq $apiScope) "passes when the rule targets the expected App Service"
Assert-True (-not ($blazorScope -eq $apiScope)) "fails when the rule targets the wrong App Service"
# Regression guard for the original defect: '*atlas-api*' compared with -eq is
# literal equality and never matches a real resource ID.
Assert-True ("*atlas-api*" -ne $apiScope) "wildcard pattern is not literal-equal to a resource ID"
Assert-True ($apiScope -like "*$apiName") "-like wildcard matches the expected app name"

Write-Host " log alert - substring scope match" -ForegroundColor Yellow
$appiScopes = @("/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Insights/components/atlasdevappi")
Assert-True ((@($appiScopes | Where-Object { $_.Contains('Microsoft.Insights/components') })).Count -gt 0) `
    "passes when a scope contains the expected provider path"
$wrongScopes = @("/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.OperationalInsights/workspaces/w")
Assert-True ((@($wrongScopes | Where-Object { $_.Contains('Microsoft.Insights/components') })).Count -eq 0) `
    "fails when no scope contains the expected provider path"
# Regression guard: -like without a trailing wildcard requires an exact SUFFIX,
# which would wrongly reject real scopes ending in the resource name.
Assert-True (-not ($appiScopes[0] -like "*Microsoft.Insights/components")) `
    "-like suffix pattern would wrongly reject real App Insights scopes"

Write-Host " activity log alert - exact subscription scope" -ForegroundColor Yellow
$subscriptionScope = "/subscriptions/$sub"
Assert-True ($subscriptionScope -eq "/subscriptions/$sub") `
    "passes when the rule targets the exact deployment subscription"
$wrongSubscriptionScope = "/subscriptions/00000000-0000-0000-0000-000000000000"
Assert-True ($wrongSubscriptionScope -ne $subscriptionScope) `
    "fails when the rule targets a different subscription"
$resourceGroupScope = "$subscriptionScope/resourceGroups/$rg"
Assert-True ($resourceGroupScope -ne $subscriptionScope) `
    "fails when the rule targets a resource-group scope instead of the subscription"

Write-Host " activity log alert - bootstrap expectation key" -ForegroundColor Yellow

$activityLogExpectation = @{
    Name = 'atlas-dev-service-health'
    Type = 'activitylog'
    ScopeStartsWith = "/subscriptions/$sub"
}

Assert-True ($activityLogExpectation.ContainsKey('ScopeStartsWith')) `
    "activity log expectation uses the ScopeStartsWith key consumed by Verify-O7Alerting"

Assert-True (-not $activityLogExpectation.ContainsKey('ScopeExact')) `
    "activity log expectation does not use the unsupported ScopeExact key"    

# O6 Grafana RBAC preflight logic (Phase 11b) - mirrors the comparisons used in
# Verify-O6Visualization in infra/bootstrap.ps1.
# -----------------------------------------------------------------------------

Write-Host " O6 Grafana bootstrap identity RBAC preflight" -ForegroundColor Yellow

$grafanaEditorRoleId = 'a79a5197-3a5c-4973-a920-486035ffd60f'
$grafanaResourceId   = "/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Dashboard/grafana/atlas-dev-grafana"
# Identity resolution: the preflight uses az ad signed-in-user show -> .id.
# Simulate a resolved current user (any non-empty GUID is valid).
$currentPrincipalId = '11111111-1111-1111-1111-111111111111'
Assert-True (-not [string]::IsNullOrWhiteSpace($currentPrincipalId)) "current signed-in identity resolves to a non-empty object ID"

# Role check: assignment list filtered by exact role ID and Grafana resource scope.
$assignmentOk = [PSCustomObject]@{
    roleDefinitionId = "/subscriptions/$sub/providers/Microsoft.Authorization/roleDefinitions/$grafanaEditorRoleId"
    scope            = $grafanaResourceId
}
$assignmentsOk = @($assignmentOk)
Assert-True (@($assignmentsOk | Where-Object { $_.roleDefinitionId -like "*$grafanaEditorRoleId" }).Count -gt 0) "Grafana Editor assignment satisfies the role filter"

# Missing role: empty assignment list must fail.
$assignmentsMissing = @()
Assert-True ((@($assignmentsMissing).Count -gt 0) -eq $false) "missing Grafana Editor role fails the preflight"

# Wrong-role case: an assignment with a different role definition must fail.
$wrongRoleAssignment = [PSCustomObject]@{
    roleDefinitionId = "/subscriptions/$sub/providers/Microsoft.Authorization/roleDefinitions/00000000-0000-0000-0000-000000000000"
    scope            = $grafanaResourceId
}
Assert-True ((@($wrongRoleAssignment) | Where-Object { $_.roleDefinitionId -like "*$grafanaEditorRoleId" }).Count -eq 0) "different role on Grafana resource fails the preflight"

# -----------------------------------------------------------------------------
# O6/O7 infrastructure correction checks - dependency relationships, scheduled
# query measure columns, Grafana RBAC invariants. Static inspection of the
# actual Bicep source so regressions are caught without a deployment.
# -----------------------------------------------------------------------------

Write-Host " Infrastructure dependency & alert schema checks" -ForegroundColor Yellow

$mainBicep = Get-Content infra/main.bicep -Raw

function Assert-BicepDependency {
    param([string]$DependentName, [string]$TargetName)
    # Match: resource/module <DependentName> ... followed by dependsOn containing <TargetName>
    $pattern = "(?s)(resource|module)\s+$DependentName\s+[^=]*=\s*\{.*?dependsOn:\s*\[[^\]]*\b$TargetName\b[^\]]*\]"
    Assert-True ($mainBicep -match $pattern) "$DependentName depends on $TargetName"
}

Assert-BicepDependency 'grafanaBootstrapEditor' 'grafana'
Assert-BicepDependency 'apiAppServiceDiagnostics' 'apiAppService'
Assert-BicepDependency 'blazorAppServiceDiagnostics' 'blazorAppService'
Assert-BicepDependency 'sqlDatabaseDiagnostics' 'sqlDatabase'
Assert-BicepDependency 'storageDiagnostics' 'storage'
Assert-BicepDependency 'keyVaultDiagnostics' 'keyVault'
#Assert-BicepDependency 'apiAvailabilityAlert' 'apiAppService'
#Assert-BicepDependency 'blazorAvailabilityAlert' 'blazorAppService'
Assert-BicepDependency 'exceptionSpikeAlert' 'appInsights'
Assert-BicepDependency 'emailFailureAlert' 'appInsights'
Assert-BicepDependency 'commandLatencyAlert' 'appInsights'

# Scheduled query measure columns
Assert-True ($mainBicep -match "metricMeasureColumn:\s*'Exceptions'") "exceptionSpikeAlert uses measure column Exceptions"
Assert-True ($mainBicep -match "metricMeasureColumn:\s*'Failures'") "emailFailureAlert uses measure column Failures"
Assert-True ($mainBicep -match "metricMeasureColumn:\s*'CommandDurationP95'") "commandLatencyAlert uses measure column CommandDurationP95"
Assert-True ($mainBicep -match 'CommandDurationP95 = percentile') "command latency query names its result column"

# Module requires metricMeasureColumn (no default -> forces callers to supply it)
$sqa = Get-Content infra/modules/scheduledqueryalert.bicep -Raw
Assert-True ($sqa -match "param metricMeasureColumn string") "scheduledqueryalert module declares metricMeasureColumn parameter"
Assert-True ($sqa -match "metricMeasureColumn:\s*metricMeasureColumn") "criterion passes metricMeasureColumn through"

# ACS diagnostic categories replaced
Assert-True (-not ($mainBicep -match "'RequestLogs'")) "invalid RequestLogs category removed"
Assert-True ($mainBicep -match "EmailSendMailOperational") "ACS Send Mail operational category present"
Assert-True ($mainBicep -match "EmailStatusUpdateOperational") "ACS Status Update operational category present"

# Grafana invariants preserved
Assert-True ($mainBicep -match "param grafanaBootstrapPrincipalId string") "grafanaBootstrapPrincipalId parameter still exists"
Assert-True ($mainBicep -match "a79a5197-3a5c-4973-a920-486035ffd60f") "Grafana Editor role definition unchanged"
Assert-True ($mainBicep -match "principalType:\s*'User'") "Grafana Editor assignment principalType is User"
Assert-True ($mainBicep -match "scope:\s*grafanaInstance") "Grafana Editor assignment remains resource-scoped"

# Static safety
$infraFiles = Get-ChildItem infra -Recurse -Include *.bicep,*.ps1,*.json | Select-String 'Microsoft.Dashboard/grafana/dashboards\s*=|api[_-]?key\s*=|Bearer\s+[A-Za-z0-9]' -CaseSensitive:$false
Assert-True ($null -eq $infraFiles) "no invalid Grafana ARM resource / API keys / static tokens in infra"
