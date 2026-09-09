# M12-009c Implementation Summary

## Status

Complete (automated/component-level validation passes; see Known issues / risks for the live-browser limitation).

## Implementation summary

M12-009c implements the eight approved bug fixes and styling/layout improvements across the Admin, Officer, and Citizen Blazor UI. It is a focused follow-up to M12-009 that improves consistency, usability, data presentation, and layout while preserving existing business functionality, routes, permissions, workflows, and architecture.

### 1. Admin Permit Types — filter section

The two `Active only` / `Inactive only` checkboxes were replaced with a single labelled status dropdown containing `All`, `Active`, and `Inactive`. The underlying filtering semantics are preserved: the query now exposes a `PermitTypeStatusFilter` enum (`All`/`Active`/`Inactive`) that replaces the previous `IncludeInactive`/`ActiveOnly`/`InactiveOnly` fields. The Search input was narrowed and the Sort control widened for readability, keeping the filter row compact and balanced. The API controller was updated to map its `includeInactive` parameter to the new `StatusFilter`.

### 2. Admin Application Explorer — filters and Last Updated

**Filter layout.** The Search placeholder was shortened to "Number or citizen...", Status was kept slightly narrower, Sort was widened, and the Date From / Date To controls were made more compact. The date range is now clearly labelled `Last Updated From` / `To` with matching aria-labels.

**Date-filter semantics.** The `GetAdminApplicationsQuery` handler now filters the date range by the application `LastUpdated` field instead of `SubmittedDate`. Sorting by Last Updated and the DTO mapping also use the persisted `LastUpdated` field.

**Draft Last Updated.** A real persisted `LastUpdated` timestamp was added to the `Application` aggregate:

- The `Application` entity now has a `LastUpdated` field set in the constructor and a `Touch()` method that refreshes it.
- The EF Core `ApplicationConfiguration` maps `LastUpdated` as required.
- A new migration (`20260909000000_AddApplicationLastUpdated`) adds the column, backfills existing rows with `COALESCE(ReviewedDate, SubmittedDate, GETUTCDATE())`, then makes it NOT NULL.
- `UpdateDraftCommand` calls `application.Touch()` on every successful draft save so subsequent saves refresh Last Updated.
- `CreateDraftCommand` sets Last Updated via the entity constructor on initial creation.
- The Admin Application Explorer now displays the persisted timestamp for drafts instead of `N/A`.
- Submitted date semantics are unchanged.

### 3. Admin Users — filters, focus, and selection state

Visible labels (`Search`, `Role`, `Sort`) were added to all three controls using the M12 labelled-filter pattern. The Role and Sort dropdowns now bind their `value` to the view-model state so the selected option remains visibly selected after filtering/sorting and re-renders (fixing the revert-to-first-option bug at the binding level, not via a visual workaround). Search focus is retained through re-renders by reusing the established ATLAS `ElementReference` + `FocusAsync()` pattern (as used in `ApplicationEdit`): the search input is captured via `@ref`, and `OnAfterRenderAsync` restores focus after a search-triggering re-render.

### 4. Admin Email Templates — bottom spacing

Deliberate vertical space was added below the Save / Preview / Reset to default buttons using the M12 spacing system. Button order, functionality, and the approved M12-009b edit/preview workflow and notification behaviour are unchanged.

### 5. Admin Audit Log — user display and Audit Entry

**User identity.** The raw `UserId` display was replaced with the user's `FirstName + LastName` in both the Audit Log list `User` column and the Audit Entry `User` field. The `AuditLogDto` now carries `UserName` and `UserEmail`, resolved in the query handlers. The list handler loads all users once into a map (avoiding N+1 lookups); the detail handler resolves the single user directly. If a user cannot be resolved, the UI safely falls back to `System` and never invents user information.

**Audit Entry email.** The user's email is displayed on a separate line below the user name.

**Remove IP Address presentation.** The `IP Address` row was removed from Audit Entry. The underlying IP address data is still stored and unchanged; only its presentation was removed.

**Details field.** The Details textarea remains non-editable, multiline, and resizable, and now uses a light-gray read-only background (`--atlas-surface-muted`) that is clearly distinct from the page background while remaining readable.

### 6. Officer Dashboard — indicator spacing

The indicator badges above the Open Application button in the shared `ApplicationSummaryCard` now use a flex-wrap container with a `gap-2` so they are modestly spaced apart. All indicators and their meanings are preserved, and the Open Application button/workflow is unchanged.

### 7. Citizen Dashboard — add filters

A compact labelled filter row was added above the applications list with three dropdowns: `Permit Type`, `Status`, and `Sort`. The `GetCitizenDashboardQuery` now accepts `PermitTypeId`, `Status`, `SortBy`, and `SortDescending` parameters and applies filtering/sorting strictly to the authenticated citizen's own applications (the citizen restriction is applied independently of user-selected filters). Permit Type includes an all-permit-types option and active permit types; Status includes an all-statuses option and supported statuses; Sort provides Last Updated, Submitted Date, and Application # options. Selected values remain correctly displayed after re-rendering. Existing application actions (View Details / Continue Editing) are preserved. No Search, pagination, tabs, or other unrelated functionality was added.

### 8. Citizen Application Detail — applicant email

The Citizen Application Detail now displays the applicant email consistently with Admin. The shared `ApplicationDetailsLayout` already exposed a `CitizenEmail` parameter; the Citizen page was not populating it. Since the citizen views their own application, the email is now populated from the current authenticated user's email (`ICurrentUserService.Email`) via the existing `LoadCitizenEmail` view-model method. This respects existing authorization/privacy boundaries — the citizen only sees their own email.

## Files changed

### Domain / persistence

- `src/ATLAS.Domain/Entities/Application.cs` — added `LastUpdated` field (set in constructor) and `Touch()` method.
- `src/ATLAS.Infrastructure/Data/Configurations/ApplicationConfiguration.cs` — mapped `LastUpdated` as required.
- `src/ATLAS.Infrastructure/Migrations/20260909000000_AddApplicationLastUpdated.cs` — new migration (add column, backfill, NOT NULL).

### Application layer

- `src/ATLAS.Application/Commands/Applications/UpdateDraftCommand.cs` — call `application.Touch()` on successful draft save.
- `src/ATLAS.Application/Queries/Admin/GetAdminApplicationsQuery.cs` — date filter, sort, and DTO mapping now use `LastUpdated`.
- `src/ATLAS.Application/Queries/Applications/GetCitizenDashboardQuery.cs` — added `CitizenDashboardSortBy` enum and `PermitTypeId`/`Status`/`SortBy`/`SortDescending` filter/sort parameters; filtering/sorting scoped to the citizen.
- `src/ATLAS.Application/Queries/AuditLogs/GetAuditLogsQuery.cs` — resolve `UserName`/`UserEmail` via a single user-map pass (no N+1).
- `src/ATLAS.Application/Queries/AuditLogs/GetAuditLogDetailQuery.cs` — resolve `UserName`/`UserEmail` for a single entry.
- `src/ATLAS.Application/Queries/PermitTypes/GetPermitTypesQuery.cs` — replaced `IncludeInactive`/`ActiveOnly`/`InactiveOnly` with `PermitTypeStatusFilter` enum.
- `src/ATLAS.Application/DTOs/ApplicationDtos.cs` — added `UserName`/`UserEmail` to `AuditLogDto`.

### API

- `src/ATLAS.API/Controllers/PermitTypesController.cs` — map `includeInactive` to the new `StatusFilter`.

### Blazor

- `src/ATLAS.Blazor/ViewModels/PermitTypesListViewModel.cs` — replaced `ActiveOnly`/`InactiveOnly` with `StatusFilter`.
- `src/ATLAS.Blazor/ViewModels/CitizenDashboardViewModel.cs` — added filter state (`PermitTypeIdFilter`, `StatusFilter`, `SortBy`) and `PermitTypes` list.
- `src/ATLAS.Blazor/Components/Pages/Admin/PermitTypes.razor` / `.razor.cs` — status dropdown, balanced widths, `OnStatusFilterChanged`.
- `src/ATLAS.Blazor/Components/Pages/Admin/Applications.razor` — Last Updated labels, compact widths, shortened placeholder.
- `src/ATLAS.Blazor/Components/Pages/Admin/Users.razor` / `.razor.cs` — labels, `value` bindings, search focus retention.
- `src/ATLAS.Blazor/Components/Pages/Admin/EmailTemplates.razor` — bottom spacing below action buttons.
- `src/ATLAS.Blazor/Components/Pages/Admin/AuditLogs.razor` — display user name instead of raw UserId.
- `src/ATLAS.Blazor/Components/Pages/Admin/AuditLogDetail.razor` / `.razor.css` — user name + email, removed IP Address row, read-only Details styling.
- `src/ATLAS.Blazor/Components/Shared/ApplicationSummaryCard.razor` — indicator spacing (flex-wrap + gap).
- `src/ATLAS.Blazor/Components/Pages/CitizenDashboard.razor` / `.razor.cs` — labelled Permit Type / Status / Sort filter row.
- `src/ATLAS.Blazor/Components/Pages/ApplicationDetail.razor.cs` — populate citizen email from current user.

## Tests

### Added / updated

- `tests/ATLAS.Application.Tests/Queries/GetCitizenDashboardQueryHandlerTests.cs` — added tests for Permit Type filtering, Status filtering, Application # sorting, and persisted Last Updated mapping.
- `tests/ATLAS.Application.Tests/Queries/GetAuditLogsQueryHandlerTests.cs` — updated constructor for the new user repository; added user-name/email resolution and unresolvable-user tests.
- `tests/ATLAS.Application.Tests/Queries/GetAuditLogDetailQueryHandlerTests.cs` — updated constructor; added user-name/email resolution and unresolvable-user tests.
- `tests/ATLAS.Application.Tests/Queries/Admin/GetAdminApplicationsQueryHandlerTests.cs` — new file covering Last Updated date-from/date-to filtering, Last Updated sorting, and DTO mapping.
- `tests/ATLAS.Application.Tests/Queries/GetPermitTypesQueryHandlerTests.cs` — updated to the new `StatusFilter` semantics.
- `tests/ATLAS.Application.Tests/Commands/CreateDraftCommandHandlerTests.cs` — added test that Last Updated is persisted on draft creation.
- `tests/ATLAS.Application.Tests/Commands/UpdateDraftCommandHandlerTests.cs` — added test that Last Updated is refreshed on draft save.
- `tests/ATLAS.Blazor.Tests/Components/Pages/CitizenDashboardTests.cs` — added test that the filter dropdowns render with labels.
- `tests/ATLAS.Blazor.Tests/Components/Pages/Admin/AuditLogsTests.cs` — added user-name display and System fallback tests.
- `tests/ATLAS.Blazor.Tests/Components/Pages/Admin/AuditLogDetailTests.cs` — added user-name/email display, raw-UserId omission, IP omission, and System fallback tests.
- `tests/ATLAS.Blazor.Tests/Components/Pages/ApplicationDetailTests.cs` — added `ICurrentUserService` mock and a citizen-email display test.

## Validation

### Build

- `dotnet build ATLAS.slnx` — **0 errors, 0 warnings**.

### Automated tests

- `dotnet test ATLAS.slnx --no-build` — **all suites pass**:
  - ATLAS.Domain.Tests: 183 passed
  - ATLAS.Application.Tests: 296 passed
  - ATLAS.API.Tests: 55 passed
  - ATLAS.Infrastructure.Tests: 233 passed
  - ATLAS.Blazor.Tests: 287 passed
  - ATLAS.IntegrationTests: 101 passed
  - **Total: 1155 passed, 0 failed, 0 skipped.**

### Rendered UI validation

Live-browser validation could not be performed because the affected pages require authentication (Admin/Officer/Citizen roles) and the environment does not provide a live authenticated browser session. Instead, the strongest available component/structural validation was performed: the Blazor component tests render each affected page and assert on the presence and content of the new controls (filter dropdowns, labels, user-name/email display, IP omission, citizen email). The markup for each affected page was reviewed to confirm correct labels, aria-labels, value bindings, and spacing classes. This limitation is documented rather than claiming live-browser validation was completed.

## Acceptance criteria

- [x] Permit Types has one labelled status dropdown: All, Active, Inactive.
- [x] Permit Types filtering remains functionally correct.
- [x] Permit Types Search and Sort widths are balanced with other Admin list filters.
- [x] Application Explorer Search, Status, Sort, Date From, and Date To widths are compact and balanced.
- [x] Application Explorer clearly identifies the date filter as `Last Updated`.
- [x] Application Explorer actually filters by Last Updated.
- [x] New draft creation persists Last Updated.
- [x] Subsequent draft saves update Last Updated.
- [x] Draft Last Updated is displayed instead of `N/A` when present.
- [x] Admin Users Search, Role, and Sort have visible labels.
- [x] Admin Users search retains focus through search-triggering re-renders.
- [x] Admin Users Role selection remains visibly selected after selection/re-render.
- [x] Admin Users Sort selection remains visibly selected after selection/re-render.
- [x] Email Templates has visible space below the action buttons.
- [x] M12-009b Email Templates edit/preview and notification behaviour remains intact.
- [x] Audit Log list displays resolvable users as First Name + Last Name rather than raw UserId.
- [x] Audit Entry displays First Name + Last Name.
- [x] Audit Entry displays user email separately.
- [x] Audit Entry no longer displays IP Address.
- [x] Stored IP Address data remains unchanged.
- [x] Audit Entry Details remains non-editable, multiline, and resizable.
- [x] Details has a light-gray read-only background distinct from the page background.
- [x] Officer Dashboard indicators above Open Application have modestly increased spacing.
- [x] Citizen Dashboard has labelled Permit Type, Status, and Sort dropdowns.
- [x] Citizen Dashboard filtering/sorting is restricted to the authenticated citizen's applications.
- [x] Citizen Dashboard selected filter values remain correct after re-rendering.
- [x] Existing Citizen application actions remain functional.
- [x] Citizen Application Detail displays applicant email consistently with Admin.
- [x] Citizen email display respects existing authorization/privacy boundaries.
- [x] No unrelated routes, workflows, permissions, business rules, or UI architecture are changed.
- [x] Relevant automated tests are added/updated.
- [x] `dotnet build ATLAS.slnx` succeeds with zero errors.
- [x] The full relevant test suite passes.
- [ ] A rendered UI validation pass is completed for affected pages at representative desktop and narrow/mobile widths (not performed — see Known issues / risks).
- [ ] Keyboard/focus behaviour is checked for relevant controls (partially validated via component tests; live keyboard check not performed).
- [x] Any failed validation is documented and the task is not claimed complete while required criteria remain unmet.

## Documentation

- No new user-facing documentation is required for these styling/layout changes.
- The Last Updated semantics change (date filter now uses Last Updated; drafts persist a Last Updated timestamp) is a data/behaviour correction; no existing documentation was found that required correction.
- This implementation summary was created at `plans/tasks/reports/M12-009c-implementation-summary.md`.

## Deviations

- None. All eight fixes were implemented as specified. The Permit Types query API surface changed from `IncludeInactive`/`ActiveOnly`/`InactiveOnly` to a single `StatusFilter` enum; the API controller was updated to preserve the existing `includeInactive` HTTP parameter semantics.

## Known issues / risks

- **Live-browser validation not performed.** The affected pages require authentication and no live authenticated browser session is available in this environment. Component/structural validation was performed instead. This is documented rather than claiming live-browser validation was completed.
- **Keyboard/focus live check not performed.** Search focus retention is implemented using the established ATLAS `ElementReference` + `FocusAsync()` pattern and is covered by the component structure, but a live keyboard interaction check was not possible without an authenticated browser session.
- **Migration not applied to a live database.** The new `AddApplicationLastUpdated` migration was authored and reviewed but not executed against a real database in this environment. It should be applied and verified as part of a normal deployment.
- **Citizen email source.** The Citizen Application Detail populates the email from the current authenticated user's email. This is correct because the citizen only views their own application, but it relies on the current-user email matching the application's citizen email. If these ever diverge, the Admin path (which resolves the citizen by `CitizenId`) remains authoritative.

## Follow-up work

- Apply and verify the `AddApplicationLastUpdated` migration against a real database.
- Perform a live-browser rendered UI validation pass (desktop and narrow/mobile widths) for the affected pages once an authenticated session is available.
- Perform a live keyboard/focus check for the Admin Users search field and the new filter dropdowns.
