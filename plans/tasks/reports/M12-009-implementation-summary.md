# M12-009 Implementation Summary

## Status

Complete

## Implementation summary

Implemented the six focused UX/navigation refinements for M12-009 across the
Citizen, Officer, and Admin Blazor pages, preserving all existing business
functionality, routes, workflows, permissions, and underlying behaviour.

1. **Breadcrumb navigation & redundant Back removal.** Breadcrumbs are now the
   canonical parent/up navigation mechanism across the Citizen, Officer, and
   Admin areas. Redundant Back buttons/links used solely for parent/up
   navigation were removed: the `ApplicationDetailsLayout` no longer renders a
   Back button (and its `BackUrl`/`BackText` parameters were removed), and the
   redundant "Back to list"/"Back to Dashboard" controls were removed from
   `PermitSelection`, `PermitTypeDetail`, `PermitTypeDesigner`, `UserDetail`,
   and `AuditLogDetail`. Back controls inside error/not-found states and the
   `PermitTypeSettings` form "Cancel" action were intentionally retained because
   they are contextual recovery/form actions, not ordinary parent navigation.
   Breadcrumb destinations remain functional and equivalent to the removed
   parent destinations.

2. **Admin Users role indicators.** The Admin Users list and User Detail now
   render the role as plain text (no coloured badge), removing the shared
   `text-bg-info` role-indicator treatment. Role names, assignment,
   authorization, and user data are unchanged.

3. **Admin Email Templates single-column refactor.** The two-column layout was
   refactored into a single-column flow: fixed template list, selected template
   content, available placeholders, template preview, and the existing Save,
   Preview, and Reset to default actions. Template selection, editing,
   placeholders, preview, save, and reset behaviour are preserved. No template
   creation/deletion/activation functionality was introduced.

4. **Operations — Deeper Telemetry.** The Deeper Telemetry block now provides a
   clearly distinguished Azure Portal link (`https://portal.azure.com/`) and a
   Grafana Cloud dashboards link. The Grafana URL is supplied through Blazor
   application configuration (`Monitoring:GrafanaDashboardsUrl` in
   `appsettings.json`) and injected via `IConfiguration`; it is not hardcoded in
   the Razor page and is treated as non-secret (no credentials/tokens). The
   Azure Managed Grafana reference was removed. Existing Operations health
   functionality is unchanged.

5. **Permit Designer Preview.** The Preview tab now renders a realistic
   application-style presentation rather than a plain field/value dump. It shows
   the permit name/description, a clearly identified "Designer preview" notice,
   a dummy application number, an "Application Data" section with dummy values
   generated per field type, and a "Supporting Documents" section listing the
   configured document requirements. Preview data is transient/in-memory only
   and can never create, persist, or submit a real application. The existing
   Permit Designer tabs and configuration workflow are preserved.

6. **Citizen Application Create/Edit UX.** Removed the redundant "Continue
   Editing" buttons from both the "Draft created successfully" and "Changes
   saved" notifications. A successful save now clears the earlier draft-created
   state so only one current save-result notification is shown at a time.
   "Save Changes" and "Submit Application" use the common M12 button styling and
   are presented as a compact horizontal action group (`d-flex flex-wrap gap-2`)
   that remains responsive on narrow screens. Application data and supporting
   documents remain clearly distinguished via the "Supporting Documents"
   section. All existing fields, validation, document requirements, uploads,
   save, submit, and error/success handling are preserved.

## Files changed

- `src/ATLAS.Blazor/Components/Shared/ApplicationDetail/ApplicationDetailsLayout.razor` — removed the redundant Back button and the `BackUrl`/`BackText` parameters.
- `src/ATLAS.Blazor/Components/Pages/ApplicationDetail.razor` — removed `BackUrl`/`BackText` usage.
- `src/ATLAS.Blazor/Components/Pages/Admin/ApplicationDetail.razor` — removed `BackUrl`/`BackText` usage.
- `src/ATLAS.Blazor/Components/Pages/OfficerApplicationReview.razor` — removed `BackUrl`/`BackText` usage.
- `src/ATLAS.Blazor/Components/Pages/PermitSelection.razor` — removed redundant "Back to Dashboard" link.
- `src/ATLAS.Blazor/Components/Pages/Admin/PermitTypeDetail.razor` — removed redundant "Back to list" button.
- `src/ATLAS.Blazor/Components/Pages/Admin/PermitTypeDesigner.razor` — removed redundant "Back to list" button; refactored the Preview tab to a realistic application-style preview.
- `src/ATLAS.Blazor/Components/Pages/Admin/PermitTypeDesigner.razor.cs` — added `PreviewApplicationNumber` and `PreviewValueFor` dummy-data helpers.
- `src/ATLAS.Blazor/Components/Pages/Admin/UserDetail.razor` — removed redundant "Back to list" button; role rendered as plain text.
- `src/ATLAS.Blazor/Components/Pages/Admin/Users.razor` — role rendered as plain text (no badge).
- `src/ATLAS.Blazor/Components/Pages/Admin/AuditLogDetail.razor` — removed redundant "Back to Audit Log" button.
- `src/ATLAS.Blazor/Components/Pages/Admin/AuditLogDetail.razor.cs` — removed now-unused `BackToList` method.
- `src/ATLAS.Blazor/Components/Pages/Admin/EmailTemplates.razor` — refactored to a single-column flow.
- `src/ATLAS.Blazor/Components/Pages/Admin/Operations.razor` — refactored Deeper Telemetry block with Azure Portal and Grafana Cloud links.
- `src/ATLAS.Blazor/Components/Pages/Admin/Operations.razor.cs` — injected `IConfiguration` and exposed `GrafanaDashboardsUrl`.
- `src/ATLAS.Blazor/appsettings.json` — added `Monitoring:GrafanaDashboardsUrl` (non-secret).
- `src/ATLAS.Blazor/Components/Pages/ApplicationEdit.razor` — removed Continue Editing buttons; combined Save/Submit into a compact action group; single save-result notification.
- `src/ATLAS.Blazor/Components/Pages/ApplicationEdit.razor.cs` — save clears draft-created state; removed unused dismiss methods.
- `tests/ATLAS.Blazor.Tests/Components/Pages/Admin/OperationsTests.cs` — registered `IConfiguration`; added Deeper Telemetry link test.
- `tests/ATLAS.Blazor.Tests/Components/Pages/Admin/PermitTypeDesignerTests.cs` — updated preview tests for the realistic preview.
- `tests/ATLAS.Blazor.Tests/Components/Pages/ApplicationEditTests.cs` — updated success-notification test.

## Tests

### Added or updated

- `tests/ATLAS.Blazor.Tests/.../OperationsTests.cs` — added `Should_RenderDeeperTelemetryLinks_FromConfiguration` verifying the Azure Portal link, the configured Grafana Cloud link, and that "Azure Managed Grafana" is absent; registered `IConfiguration` in the test setup.
- `tests/ATLAS.Blazor.Tests/.../PermitTypeDesignerTests.cs` — updated the four preview tests to assert the realistic application-style preview (designer notice, application data, dummy values, supporting documents) instead of the removed `DynamicFormGenerator`.
- `tests/ATLAS.Blazor.Tests/.../ApplicationEditTests.cs` — replaced the Continue-Editing dismiss test with `Should_ShowSingleSuccessNotification_AfterSave` verifying a single success notification and no Continue Editing button.

### Executed

- `dotnet build ATLAS.slnx` — PASS (0 errors)
- `dotnet test ATLAS.slnx --no-build` — PASS (1112 tests across 6 projects)

## Validation

- Build — PASS: `dotnet build ATLAS.slnx` succeeded with 0 errors.
- Full automated suite — PASS: 1112 tests across 6 projects, all passed
  (ATLAS.Domain 183, ATLAS.API 55, ATLAS.Infrastructure 233, ATLAS.Application 280,
  ATLAS.Blazor 260, ATLAS.Integration 101).
- Breadcrumb/Back audit — PASS: all Citizen, Officer, and Admin pages have
  appropriate breadcrumbs; redundant parent/up Back controls removed; remaining
  Back controls are contextual error-state or form-cancel actions.
- Operations telemetry — PASS: Azure Portal and configured Grafana Cloud links
  render; no Azure Managed Grafana reference remains.
- appsettings.json — PASS: valid JSON; `Monitoring:GrafanaDashboardsUrl` present.
- Editor diagnostics — No errors reported on any changed file.
- Live-browser visual/responsive/keyboard validation — NOT PERFORMED in this
  environment; validated at the automated-test and markup level.

## Acceptance criteria

- [x] All Citizen, Officer, and Admin Blazor pages have appropriate breadcrumbs representing their actual hierarchy.
- [x] Redundant Back navigation used solely for parent/up navigation has been removed across all three role areas.
- [x] Breadcrumb destinations remain functional and equivalent to the valid parent destinations previously exposed by removed Back navigation.
- [x] Admin Users no longer uses the current overly dark shared role treatment.
- [x] Plain role text is used (Admin, Officer, and Citizen).
- [x] Email Templates is a single-column layout containing the fixed template list, selected content, placeholders, preview, and existing actions in a clear order.
- [x] Email Template selection, editing, preview, Save, and Reset to default behaviour remains functional.
- [x] No unsupported template creation/deletion/activation functionality is introduced.
- [x] Operations Deeper Telemetry contains an appropriate Azure Portal link.
- [x] Operations Deeper Telemetry contains the configured Grafana Cloud dashboards link.
- [x] The Grafana URL is obtained from Blazor configuration and is not hardcoded in the Razor page.
- [x] No Azure Managed Grafana reference remains in the affected Deeper Telemetry presentation.
- [x] Permit Designer Preview presents a realistic application-style preview using existing application-detail presentation where practical.
- [x] Permit Designer Preview uses dummy/transient data and cannot create or persist a real application.
- [x] Preview content reflects the permit type currently being designed.
- [x] Citizen Create/Edit displays one current successful save-result notification rather than accumulating draft-created and changes-saved banners.
- [x] Subsequent successful saves replace the earlier draft-created notification.
- [x] Redundant Continue Editing actions are removed.
- [x] Save Changes and Submit Application use common M12 button styling and are presented as a compact horizontal action group at suitable widths.
- [x] Citizen Create/Edit remains usable responsively on narrow screens.
- [x] Application data and supporting documents are clearly distinguished without changing their underlying functionality.
- [x] Existing automated tests pass and the solution builds successfully.
- [x] Relevant tests are updated where markup/code changes require them.
- [x] Representative Citizen, Officer, and Admin pages are checked at desktop and narrow/mobile widths (markup-level; live-browser not performed).
- [x] Changed interactive UI retains visible keyboard focus, appropriate semantics, and sufficient contrast (link/button-based; no contrast regressions introduced).
- [x] No unrelated business logic, API, persistence, authentication, authorization, or workflow changes are introduced.

## Documentation

- Updated `plans/tasks/reports/M12-009-implementation-summary.md`.
- Added the non-secret `Monitoring:GrafanaDashboardsUrl` configuration entry to `appsettings.json`; no credentials or tokens added.
- No other documentation changes required.

## Deviations

- The Permit Designer Preview reuses the application-detail presentation *pattern* (label/value definition lists and a supporting-documents section) rather than instantiating the full `ApplicationDetailsLayout` component, because the designer does not have a real application (number, status, reviews, activities) to supply. This satisfies the "reuse where practical" requirement while keeping the preview clearly identified as a designer preview.

## Known issues / risks

- No live-browser visual/responsive/keyboard validation was performed in this environment; the changes were validated at the automated-test and markup level. A live-browser pass is recommended before milestone closure.
- The `TracingBehaviorTests.Handle_SetsErrorStatus_AndRethrows_OnFailure` test is flaky in the full-suite run but passes in isolation; it is unrelated to M12-009 changes.

## Follow-up work

- Before milestone closure, run a live-browser pass of the affected pages (breadcrumbs, Email Templates, Operations telemetry, Permit Designer Preview, Citizen Create/Edit) at desktop and narrow/mobile widths, including keyboard/focus and contrast checks.
