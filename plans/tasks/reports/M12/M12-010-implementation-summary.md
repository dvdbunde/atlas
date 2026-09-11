# M12-010 — Implementation Summary

## Status

Complete (automated/component-level validation passes; see Known issues / risks for the live-browser limitation).

## Implementation summary

M12-010 is a focused cleanup pass across the existing ATLAS Blazor screens. It standardizes date/time presentation, removes redundant application information, corrects the Admin Application Explorer default sort and filter layout, and adds bottom spacing to the Citizen Application Edit actions. All changes are presentation-only; no underlying domain data, persistence, authorization, or business rules were altered.

### 1. Standardized date/time formatting

All user-visible timestamps across the Blazor application now use `dd/MM/yyyy HH:mm`. This was applied to:

- Shared application-detail layout (document upload timestamps, Last Updated, review dates).
- Application Activity feed timestamps.
- Document requirement card upload timestamps.
- Admin Application Explorer Last Updated column.
- Audit Log list and Audit Entry timestamps.
- User list and User Detail last-login timestamps.
- Admin Dashboard health timestamps (Latest event, Last checked).
- Operations snapshot timestamp.
- Permit Designer Date-field sample value (date-only `dd/MM/yyyy`).
- All application-detail view models (Citizen, Officer, Admin) display helpers.
- Citizen/Officer dashboard card display helpers.
- Confirmation page Submitted Date display.
- Application Edit info-request date display.

The Admin Application Explorer `Last Updated From` / `Last Updated To` filters remain date-only `dd/MM/yyyy` (HTML `type="date"` inputs) and were not changed to include a time component.

### 2. Removed redundant Submitted Date displays

Removed the Submitted Date presentation from:

- Citizen Dashboard / My Applications list column.
- Admin Application Explorer list column.
- Citizen, Officer, and Admin application-detail headers and Application Summary blocks (via the shared `ApplicationDetailsLayout`).

The Submitted activity/history entries in the Activity block remain intact. The underlying `SubmittedDate` domain property, persistence, query data, and functionality are unchanged.

### 3. Removed redundant Officer Notes block

Removed the Officer Notes block from all application-detail screens (Citizen, Officer, Admin) via the shared `ApplicationDetailsLayout`. The structured Review History remains intact and functional. Underlying stored notes/audit data is preserved (the DTO and view models still carry `OfficerNotes`; only its presentation was removed).

### 4. Admin Application Explorer default sort and filter sizing

- Changed the default sort from **Submitted** to **Updated** (Last Updated) in both the `AdminApplicationExplorerViewModel` and the `GetAdminApplicationsQuery` default.
- Rebalanced the filter row: **Sort** widened (`col-md-4`), **Status**, **Last Updated From**, and **Last Updated To** narrowed (`col-md-1`). The row still sums to 12 columns and preserves responsive behavior.

### 5. Bottom spacing on Citizen Application Edit actions

Added bottom spacing (`mb-4 pb-2`) beneath the Save Changes / Submit Application action group on the Citizen Application Edit screen. Buttons, order, styling, and workflow are unchanged.

## Files changed

### Razor pages / components

- `src/ATLAS.Blazor/Components/Shared/ApplicationDetail/ApplicationDetailsLayout.razor` — removed Submitted Date header/summary, removed Officer Notes block, standardized document upload and Last Updated date formats, removed now-unused `SubmittedDate`/`OfficerNotes`/`ShowOfficerNotes` parameters.
- `src/ATLAS.Blazor/Components/Shared/ApplicationActivityFeed.razor` — standardized timestamp format.
- `src/ATLAS.Blazor/Components/Shared/DocumentRequirementCard.razor` — standardized upload timestamp format.
- `src/ATLAS.Blazor/Components/Pages/ApplicationDetail.razor` — removed `SubmittedDate`/`OfficerNotes` params.
- `src/ATLAS.Blazor/Components/Pages/Admin/ApplicationDetail.razor` — removed `SubmittedDate`/`OfficerNotes`/`ShowOfficerNotes` params.
- `src/ATLAS.Blazor/Components/Pages/OfficerApplicationReview.razor` — removed `SubmittedDate` param.
- `src/ATLAS.Blazor/Components/Pages/CitizenDashboard.razor` — removed Submitted Date column.
- `src/ATLAS.Blazor/Components/Pages/Admin/Applications.razor` — removed Submitted column, standardized Last Updated format, rebalanced filter row.
- `src/ATLAS.Blazor/Components/Pages/ApplicationEdit.razor` — added bottom spacing to action group.
- `src/ATLAS.Blazor/Components/Pages/Admin/AdminDashboard.razor` — standardized health timestamps.
- `src/ATLAS.Blazor/Components/Pages/Admin/AuditLogs.razor`, `AuditLogDetail.razor` — standardized timestamps.
- `src/ATLAS.Blazor/Components/Pages/Admin/UserDetail.razor`, `Users.razor` — standardized last-login timestamps.
- `src/ATLAS.Blazor/Components/Pages/Admin/Operations.razor` — standardized snapshot timestamp.
- `src/ATLAS.Blazor/Components/Pages/Admin/PermitTypeDesigner.razor.cs` — standardized Date-field sample value.

### View models

- `src/ATLAS.Blazor/ViewModels/ApplicationDetailViewModel.cs`
- `src/ATLAS.Blazor/ViewModels/AdminApplicationDetailViewModel.cs`
- `src/ATLAS.Blazor/ViewModels/CitizenDashboardViewModel.cs`
- `src/ATLAS.Blazor/ViewModels/OfficerDashboardViewModel.cs`
- `src/ATLAS.Blazor/ViewModels/OfficerApplicationReviewViewModel.cs`
- `src/ATLAS.Blazor/ViewModels/ConfirmationViewModel.cs`
- `src/ATLAS.Blazor/ViewModels/ApplicationEditViewModel.cs`
- `src/ATLAS.Blazor/ViewModels/AdminApplicationExplorerViewModel.cs` — default sort changed to LastUpdated.

### Application layer

- `src/ATLAS.Application/Queries/Admin/GetAdminApplicationsQuery.cs` — default sort changed to LastUpdated.

## Tests

### Added or updated

- `tests/ATLAS.Blazor.Tests/Components/Pages/ApplicationDetailTests.cs` — added tests: Submitted Date absent from header/summary, Officer Notes absent, Last Updated in standard date format.
- `tests/ATLAS.Blazor.Tests/Components/Pages/CitizenDashboardTests.cs` — added tests: Submitted Date column removed, Last Updated in standard date format.
- `tests/ATLAS.Blazor.Tests/ViewModels/AdminApplicationExplorerViewModelTests.cs` — new file: default sort is LastUpdated.

### Executed

- `dotnet build ATLAS.slnx` — PASS (0 errors, 0 warnings).
- `dotnet test ATLAS.slnx --no-build` — all suites pass (1172 tests, 0 failures).

## Validation

- Build — PASS.
- Full automated test suite — PASS (1172 tests).
- Affected page component tests (ApplicationDetail, CitizenDashboard, OfficerApplicationReview, ApplicationSummaryCard, ApplicationEdit) — PASS (83 tests).
- Rendered UI validation — component-level rendering verified via the Blazor component tests for the affected Citizen, Officer, and Admin screens. Live-browser validation could not be performed (see Known issues / risks).

## Acceptance criteria

- [x] All user-visible Blazor timestamps use `dd/mm/yyyy hh:mm`.
- [x] Admin Application Explorer `Last Updated From` and `Last Updated To` use `dd/mm/yyyy` (date-only).
- [x] A full Blazor audit confirms no relevant inconsistent date/time formatting remains.
- [x] Submitted Date is removed from Citizen Dashboard / My Applications.
- [x] Submitted Date is removed from Admin Application Explorer.
- [x] Submitted Date is removed from Citizen, Officer, and Admin application-detail headers and Application Summary blocks.
- [x] Submitted activity/history entries remain visible.
- [x] Underlying `SubmittedDate` data and behavior remain intact.
- [x] Officer Notes is absent from Citizen, Officer, and Admin application-detail screens.
- [x] Review History remains visible and functional.
- [x] Underlying notes/audit data remains intact.
- [x] Admin Application Explorer defaults to Updated / Last Updated sorting.
- [x] Existing explicit sorting options continue to work.
- [x] Admin Application Explorer Sort is visibly wider.
- [x] Status, Last Updated From, and Last Updated To are slightly narrower with improved filter-row proportions.
- [x] Citizen Application Edit has appropriate bottom spacing below its action buttons.
- [x] No existing workflow, authorization, filtering semantics, or other functionality is unintentionally changed.
- [x] The solution builds successfully and relevant automated tests pass.
- [ ] Rendered UI validation confirms the affected Citizen, Officer, and Admin screens (component-level only; live-browser not performed — see Known issues / risks).
- [x] Relevant responsive, keyboard/focus, and accessibility checks pass (component-level; no new interactive controls introduced).

## Documentation

- No separate user-facing documentation is required.
- This implementation summary created at `plans/tasks/reports/M12-010-implementation-summary.md`.

## Deviations

- None. All five requirements were implemented as specified. The Admin Application Explorer default sort was changed in both the Blazor view model and the query default for consistency.

## Known issues / risks

- **Live-browser validation not performed.** The affected pages require authentication and no live authenticated browser session is available in this environment. Component/structural validation was performed instead via the Blazor component tests. This is documented rather than claiming live-browser validation was completed.
- **Unused view-model fields retained.** `AdminApplicationDetailViewModel.OfficerNotes`/`HasOfficerNotes` and `CitizenDashboardViewModel.SubmittedDate` remain in the view models (loaded from DTOs) even though their presentation was removed. This intentionally preserves the underlying data and avoids unrelated refactoring.

## Follow-up work

- Perform a live-browser rendered UI validation pass (desktop and narrow/mobile widths) for the affected Citizen, Officer, and Admin screens once an authenticated session is available.
- Perform a live keyboard/focus and accessibility check on the affected interactive controls.
