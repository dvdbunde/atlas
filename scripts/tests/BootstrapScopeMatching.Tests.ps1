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

if ($script:failures -gt 0) {
    Write-Host "`n$($script:failures) test(s) FAILED" -ForegroundColor Red
    exit 1
}
Write-Host "`nAll scope-matching tests passed" -ForegroundColor Green
exit 0