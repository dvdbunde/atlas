# M12-008 Implementation Summary

## Status

Complete

## Review corrections (round 2)

Two review corrections were applied on top of the initial implementation:

1. **Deterministic application-status rendering.** The Applications block now
   renders statuses in the defined lifecycle order
   (`Draft → Submitted → Under Review → Info Requested → Resubmitted → Approved →
   Rejected`), showing only non-zero counts. The applied state is produced and
   consumed in that fixed order:
   - In `GetAdminDashboardQueryHandler`, `ApplicationStatusCounts` is built by
     iterating a fixed `DashboardStatusOrder` array (the defined seven-status
     set) and emitting only non-zero counts, instead of relying on raw
     `GroupBy` enumeration order.
   - In `AdminDashboard.razor`, the Applications block iterates a fixed
     `DashboardApplicationStatuses` array (the same lifecycle set) and renders a
     row only when the count is non-zero. This makes rendering deterministic
     regardless of dictionary enumeration order.
   - Added an ordering assertion in `Handle_ShouldCountApplicationsByDefinedStatuses`
     verifying the returned keys follow `Submitted` before `Approved`.
2. **Cancellation must propagate.** The existing best-effort behaviour for Audit
   Logs and Operations failures is preserved, but `OperationCanceledException`
   is no longer swallowed. Each best-effort try/catch block now re-throws
   `OperationCanceledException` before falling back to the supporting-metric
   fallback. Genuine retrieval failures (e.g. `InvalidOperationException`) remain
   best-effort. Added two tests verifying cancellation from the audit query and
   from the operations query propagate out of the handler.

## Implementation summary

Refactored the Administration Dashboard (`/admin`) from a single static
summary into a functional dashboard composed of exactly six clickable summary
blocks, each linking to its corresponding Admin page. The refactor reuses the
existing ATLAS repositories and queries to aggregate live data, keeps the
existing `[Authorize(Roles = "Admin")]` route/authorization boundary intact,
and is styled with the M12 semantic token system so the blocks are usable and
keyboard-reachable on desktop and narrow/mobile viewports.

The six dashboard blocks are:

1. **Permit Types** (`/admin/permit-types`) — total in the title, with an
   Active / Inactive body split.
2. **Applications** (`/admin/applications`) — total in the title, with a body
   showing only the non-zero statuses from the defined Draft / Submitted /
   Under Review / Info Requested / Resubmitted / Approved / Rejected set.
3. **Users** (`/admin/users`) — total in the title, with Citizens / Officers /
   Administrators breakdown.
4. **Audit Logs** (`/admin/audit-logs`) — last-24h event count and the latest
   event timestamp, reusing the existing `GetAuditLogsQuery`.
5. **Email Templates** (`/admin/email-templates`) — total only (no
   active/inactive distinction).
6. **Operations** (`/admin/operations`) — system-health badge and last-checked
   timestamp, reusing the existing `GetOperationsOverviewQuery` health
   determination.

Each block is an `<a href="..." class="dashboard-block">` wrapping a Bootstrap
card, so the complete block is clickable and keyboard reachable. Dashboard
aggregation is performed in `GetAdminDashboardQueryHandler`, which now
re-invokes the existing `GetAuditLogsQuery` and `GetOperationsOverviewQuery` via
an injected `IMediator` (best-effort with try/catch tolerance) and reuses the
existing repositories for permit types, applications, and users.

## Files changed

- `src/ATLAS.Application/Queries/Admin/GetAdminDashboardQuery.cs` — extended
  `AdminDashboardDto` with `ActivePermitTypeCount`, `InactivePermitTypeCount`,
  `ApplicationStatusCounts` (non-zero only), `UserCount`, `AuditLogEventsLast24Hours`,
  `LatestAuditEventUtc`, `EmailTemplateCount` (renamed from `ActiveEmailTemplateCount`),
  `OverallHealth`, `HealthCheckedAtUtc`; extended the handler to aggregate counts and
  reuse `GetAuditLogsQuery` / `GetOperationsOverviewQuery` via injected `IMediator`.
  Review correction: `ApplicationStatusCounts` is now built in the defined lifecycle
  order (`DashboardStatusOrder`), and `OperationCanceledException` is re-thrown
  (not swallowed) from the audit and operations best-effort blocks.
- `src/ATLAS.Blazor/Components/Pages/Admin/AdminDashboard.razor` — rewritten with
  six clickable `<a href="...">` summary blocks; added `StatusLabel` and
  `HealthBadgeClass` helpers. Review correction: the Applications block now renders
  statuses deterministically in the defined lifecycle order
  (`DashboardApplicationStatuses`) and omits zero-count statuses.
- `src/ATLAS.Blazor/Components/Pages/Admin/AdminDashboard.razor.css` — styled the
  `.dashboard-block` link, title, and 2-column definition list for hover/active
  states and full-height clickable blocks.
- `src/ATLAS.Blazor/Components/Pages/Admin/AdminDashboard.razor.cs` — updated to
  load the dashboard via the extended query.
- `tests/ATLAS.Application.Tests/Queries/Admin/GetAdminDashboardQueryHandlerTests.cs` —
  updated for the new `IMediator` constructor parameter and asserted the new
  aggregation fields.
- `tests/ATLAS.Blazor.Tests/Components/Pages/Admin/AdminDashboardTests.cs` — updated
  `SampleSummary()` to the new DTO shape and added/updated assertions for the six
  blocks, titles with totals, breakdowns, non-zero statuses, and per-block links.
- `tests/ATLAS.Blazor.Tests/Components/Pages/Admin/AdminAuthorizationTests.cs` —
  replaced the renamed `ActiveEmailTemplateCount` with `EmailTemplateCount`.

## Tests

### Added or updated

- `tests/ATLAS.Application.Tests/.../GetAdminDashboardQueryHandlerTests.cs` — updated
  handler tests: summary counts, active/inactive split, user breakdown, non-zero
  status aggregation, audit recent-activity reuse, operations health reuse, and
  best-effort failure tolerance. Review correction: added a lifecycle-order
  assertion to the defined-statuses test, and added two tests verifying
  `OperationCanceledException` from the audit query and from the operations query
  each propagate out of the handler.
- `tests/ATLAS.Blazor.Tests/.../AdminDashboardTests.cs` — updated page tests:
  loading indicator, six card blocks, titles with totals, permit-type body counts,
  user breakdown, non-zero application-statuses-only rendering, audit/operations
  metrics, per-block navigation links, error state, and page header.
- `tests/ATLAS.Blazor.Tests/.../AdminAuthorizationTests.cs` — fixed DTO property
  name to match the rename.

### Executed

- `dotnet build ATLAS.slnx` — PASS (0 errors)
- `dotnet test ATLAS.slnx --no-build` — PASS (1111 tests across 6 projects)

## Validation

- Build — PASS: `dotnet build ATLAS.slnx` succeeded with 0 errors.
- Full automated suite — PASS: 1111 tests across 6 projects, all passed
  (ATLAS.Domain 183, ATLAS.API 55, ATLAS.Infrastructure 233, ATLAS.Application 280,
  ATLAS.Blazor 259, ATLAS.Integration 101).
- Blazor AdminDashboard page tests — PASS (12 tests, all pass).
- Editor diagnostics — No errors reported on any changed file.

## Acceptance criteria

- [x] Exactly six summary blocks: Permit Types, Applications, Users, Audit Logs,
      Email Templates, Operations.
- [x] Each complete block is clickable and links to its destination page.
- [x] Permit Types block: total in title plus Active / Inactive body.
- [x] Applications block: total in title plus non-zero statuses only (Draft /
      Submitted / Under Review / Info Requested / Resubmitted / Approved / Rejected).
- [x] Users block: total in title plus Citizens / Officers / Administrators.
- [x] Audit Logs block: last-24h count and latest event, reusing existing query.
- [x] Email Templates block: total only (no active/inactive distinction).
- [x] Operations block: reuses existing health determination and shows last checked.
- [x] Existing Admin route/authorization (`/admin`, `[Authorize(Roles = "Admin")]`)
      unchanged.
- [x] No unrelated UI, analytics, or pages added.
- [x] Uses the M12 visual system (ATLAS tokens/surfaces via Bootstrap and app.css).
- [x] Six blocks usable at desktop and narrow/mobile viewports (responsive grid).
- [x] Six blocks keyboard reachable with visible focus (link-based blocks).
- [x] Automated tests for the new dashboard aggregation added/updated.
- [x] Relevant Blazor component + integration test suites pass.
- [x] Required build and repository validation pass.
- [x] `plans/tasks/reports/M12-008-implementation-summary.md` created.

## Documentation

- Updated `plans/tasks/reports/M12-008-implementation-summary.md`.
- No other documentation changes required; Admin routes and authorization are unchanged.

## Deviations

None.

## Known issues / risks

- No live-browser visual validation was performed in this environment; the tile
  layout was validated at the CSS and automated-test level.

## Follow-up work

- Referenced-in M12 milestone review, run a live-browser pass of the dashboard on
  desktop and narrow viewports if desired.
