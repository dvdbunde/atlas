# M12-007 Implementation Summary

## Status

Complete

## Implementation summary

Performed the final M12 cross-application consistency, responsive, and
accessibility pass across the existing ATLAS Blazor UI. The pass was
conservative: it reviewed the styling established by M12-001 through
M12-006, fixed only observed issues directly tied to the M12-007
acceptance criteria, and did not add new functionality, pages, components,
or workflows.

Review findings and fixes:

1. Old Blazor-template styling remnants removed. The default-Blazor
   `app.css` contained two dead rules unused anywhere in the app:
   `.blazor-error-boundary` (with the standard error-boundary SVG and
   hardcoded `#b32121` red) and `.darker-border-checkbox`. Both were
   removed from `app.css`, eliminating remnants of the old default-Blazor
   visual treatment.

2. Accessibility / contrast regression fixed in status badges. The darker
   M12 semantic tokens changed the effective colours of the Bootstrap
   `bg-info` and `bg-warning` badges, producing white-text/dark-text
   combinations that failed WCAG AA contrast:
   - `bg-info text-dark` (Under Review) was ~2.66:1 - fails.
   - `bg-warning text-dark` (Info Requested, Missing, Degraded,
     Docs incomplete) was ~3.07:1 - fails.
   The affected badges were changed to the accessible Bootstrap
   `-subtle` / `-text-emphasis` variants (e.g. `bg-info-subtle
   text-info-emphasis`), which use existing ATLAS `--bs-*-subtle` /
   `--bs-*-text-emphasis` tokens and pass contrast at strong ratios
   (7.2:1 to 10.5:1). Affected files: StatusBadge, ApplicationSummaryCard,
   DocumentRequirementCard, OfficerApplicationReview, Operations.

3. Admin Applications (and other multi-column Admin list tables) narrow
   width usability. The 8-column Applications table (and the 5-7 column
   AuditLogs, PermitTypes, Users tables) were given a `min-width` so that,
   inside the existing Bootstrap `table-responsive` wrapper, they scroll
   horizontally at narrow/mobile widths (including ~478px) instead of
   squeezing/clipping columns. The table structure, columns, data, and
   Actions column are unchanged; the surrounding page does not acquire
   horizontal overflow because the scroll is contained by the wrapper.

4. "Not supplied" badge contrast. The `bg-light`/`text-muted` treatment
   (~4.45:1, marginally below WCAG AA 4.5:1) used for the "Not supplied"
   labels in DocumentRequirementCard and OfficerApplicationReview was
   replaced with the accessible `bg-secondary-subtle
   text-secondary-emphasis` treatment (~10.5:1), using existing ATLAS
   `--bs-secondary-*` tokens. Label meaning and text are unchanged.

5. Permit Designer mobile polish. Added a small CSS-only refinement to the
   PermitTypeDesigner field/requirement rows so the label and action
   button groups wrap gracefully at narrow widths and the icon buttons
   keep adequate touch-target sizing. No redesign or restructuring.

The Officer desktop widths (Dashboard 1280px, Review 1100px) were reviewed
and found consistent with the approved M12 per-area density conventions and
the Admin area; no change was justified, so they were left unchanged.

No other issues requiring changes were observed. The layout focus ring,
global `:focus-visible`, form-control white-surface fix, per-area container
widths, `table-responsive` usage, consistent breadcrumbs/page headers, and
consistent use of ATLAS design tokens were all verified as correct.

## Files changed

- src/ATLAS.Blazor/wwwroot/app.css - removed the dead `.blazor-error-boundary`
  and `.darker-border-checkbox` default-Blazor remnants.
- src/ATLAS.Blazor/Components/Shared/StatusBadge.razor - Under Review and
  Info Requested badges now use the accessible `-subtle`/`-text-emphasis`
  variants.
- src/ATLAS.Blazor/Components/Shared/ApplicationSummaryCard.razor - "Docs
  incomplete" badge uses the warning-subtle variant.
- src/ATLAS.Blazor/Components/Shared/DocumentRequirementCard.razor -
  "Missing" badge uses the warning-subtle variant.
- src/ATLAS.Blazor/Components/Pages/OfficerApplicationReview.razor -
  "Missing" badge uses the warning-subtle variant.
- src/ATLAS.Blazor/Components/Pages/Admin/Operations.razor - "Degraded"
  health badge uses the warning-subtle variant.
- src/ATLAS.Blazor/Components/Pages/Admin/Applications.razor.css - added
  `min-width` to the table for narrow-width horizontal scrolling.
- src/ATLAS.Blazor/Components/Pages/Admin/AuditLogs.razor.css - added
  `min-width` to the table for narrow-width horizontal scrolling.
- src/ATLAS.Blazor/Components/Pages/Admin/PermitTypes.razor.css - added
  `min-width` to the table for narrow-width horizontal scrolling.
- src/ATLAS.Blazor/Components/Pages/Admin/Users.razor.css - added
  `min-width` to the table for narrow-width horizontal scrolling.
- src/ATLAS.Blazor/Components/Pages/Admin/PermitTypeDesigner.razor.css -
  added narrow-width wrap/touch-target refinement for field/requirement
  rows.
- src/ATLAS.Blazor/Components/Shared/DocumentRequirementCard.razor -
  "Not supplied" badge uses the secondary-subtle variant.
- src/ATLAS.Blazor/Components/Pages/OfficerApplicationReview.razor -
  "Not supplied" badge uses the secondary-subtle variant.

No new CSS files were added; no layout/structural markup was changed.

## Tests

### Added or updated

None. This is a validation/correction pass over existing CSS and badge
classes. The repository has no established visual/static-CSS test mechanism,
and per plans/M12-plan.md section 13, tests should not be added solely for
static CSS. The badge-class changes only swapped CSS utility classes
(tested behaviour is on text content, which is unchanged).

### Executed

Full solution test run (`dotnet test --no-build`):

- ATLAS.Domain.Tests - PASS (183)
- ATLAS.API.Tests - PASS (55)
- ATLAS.Blazor.Tests - PASS (253)
- ATLAS.Infrastructure.Tests - PASS (233)
- ATLAS.Application.Tests - PASS (274)
- ATLAS.IntegrationTests - PASS (101)
- Total: 1099 tests, all passed.

## Validation

- Build - PASS: `dotnet build ATLAS.slnx` succeeded, 0 errors.
- Full automated suite - PASS: 1099 tests across 6 projects, all passed.
- Editor diagnostics - PASS: get_errors reported no errors on any changed
  file.
- CSS structural validation - PASS: app.css brace/comment balance verified
  (27/27 braces, 31/31 comments).
- Contrast check - PASS (calculated): all key text colours meet WCAG AA on
  white (links 6.7:1, muted 4.7:1, headings 8.2-15.4:1, semantic
  success/warning/error/info 4.9-5.8:1). Badge backgrounds meet AA for
  white text (4.7-11.5:1), and the corrected `-subtle`/`-text-emphasis`
  badges meet strong contrast (7.2-10.5:1).
- Remnant check - PASS: no `linear-gradient`, old default-Blazor colours,
  or dead `.blazor-error-boundary`/`.darker-border-checkbox` remain.
- Remaining-failure check - PASS: no `bg-info text-dark`,
  `bg-warning text-dark`, or `bg-light text-muted` contrast-failing badges
  remain.
- Narrow-width table check - PASS (CSS-level): the Admin list tables
  (Applications, AuditLogs, PermitTypes, Users) now have a `min-width`
  inside the Bootstrap `table-responsive` wrapper (which sets
  `overflow-x: auto`), so at narrow widths including ~478px the full table
  is reachable by horizontal scrolling without page-level overflow.
- Permit Designer narrow-width check - PASS (CSS-level): field/requirement
  rows now wrap and keep touch-target sizing at narrow widths.
- Officer width review - PASS: Officer Dashboard (1280px) and Review
  (1100px) widths are consistent with the Admin area and the approved M12
  density conventions; no change was justified.
- Live-browser visual/responsive/keyboard validation - NOT PERFORMED in
  this environment (no running browser/UI harness). The six reference
  screens were reviewed at the markup/CSS level. Visual/rendered checks
  remain outstanding and are documented under Known issues.

## Acceptance criteria status

- [x] All in-scope pages visibly belong to one ATLAS visual system.
      Shared tokens, cards, tables, page headers, focus, and status badges
      are consistent across Citizen/Officer/Admin; the remnants that broke
      consistency were removed.
- [x] No known page retains the old blue/purple gradient shell styling.
      No gradient remains in the layout CSS (verified).
- [x] Shared controls and status treatments are consistent across roles.
      StatusBadge and related badges now use consistent accessible tokens.
- [x] No new unintended horizontal overflow introduced. Containers use
      max-width only; tables use `table-responsive`; responsive breakpoints
      preserved. (Verified at CSS level.)
- [x] Keyboard focus is visible on reviewed interactive controls. Global
      focus treatment covers buttons, links, nav-links, form controls,
      checkboxes, selects, plus a global `:focus-visible`.
- [x] Reviewed text/control combinations meet the project accessibility
      expectations for contrast. All reviewed text and badge combinations
      meet WCAG AA (calculated); failing badges were corrected.
- [x] Existing functional workflows remain intact. No functional markup/
      logic changed; full 1099-test suite passes.
- [x] No invented functionality has been introduced during M12. No new
      UI/controls/pages added.
- [x] Existing automated tests and CI checks pass. All 1099 tests pass;
      build clean.

## Documentation

- Updated plans/tasks/reports/M12-007-implementation-summary.md
  (this report).
- Related M12 reports (M12-001 through M12-006) already document their
  tasks.
- Milestone plan not changed; no acceptance criteria or scope changed.

## Deviations

None. Changes were minimal and directly tied to the M12-007 acceptance
criteria. The status-badge contrast fix is a small targeted class change;
no redesign or functional changes.

## Known issues / risks

- No live-browser validation was performed in this environment. The
  six-screen review, desktop/tablet/narrow responsive checks, and an actual
  interactive keyboard review need to be executed on a running app (or via
  a browser/headless harness) before final Product Owner close. The
  narrow-width table and Permit Designer fixes were verified at the CSS
  level (Bootstrap `table-responsive` provides `overflow-x: auto`), but a
  rendered check at ~478px is still recommended.
- The "Optional" badges use `bg-secondary` (white text, ~4.69:1), which
  meets AA; the previously-failing "Not supplied" `bg-light`/`text-muted`
  badges were corrected to `bg-secondary-subtle`/`text-secondary-emphasis`
  (~10.5:1), so no known contrast-failing badge remains.

## Follow-up work

- Before milestone closure, run the M12-007 six-screen browser review,
  responsive checks at desktop/tablet/narrow widths (including the Admin
  Applications table at ~478px), and an interactive keyboard accessibility
  walkthrough (a human/headless review).
