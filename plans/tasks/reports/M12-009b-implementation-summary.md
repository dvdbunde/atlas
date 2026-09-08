# M12-009b Implementation Summary

## Status

Complete (automated/component-level validation passes; see Known issues for the live-browser limitation).

## Implementation summary

M12-009b originally refined two M12-009 areas: the Admin Email Templates page and
the Admin Permit Designer Preview. Live review subsequently accepted the Permit
Designer Preview portion, so this final iteration focuses **only** on the Admin
Email Templates page. The Permit Designer Preview and all other accepted M12-009 /
M12-009b areas were left untouched.

The Email Templates page now presents editing and previewing as **mutually
exclusive UI modes**, delivering the workflow:
**Select template → Edit → Preview → Edit**.

### Email Templates — edit / preview modes

- **Edit mode (default).** Selecting a template enters edit mode, which shows the
  fixed template selector, selected-template details, the auto-sized template
  content editor, Available placeholders, and the Save / Preview / Reset to
  default action group — in that order, with the action buttons at the bottom of
  the workflow (after placeholders, not immediately below the textarea).
- **Auto-sized editor.** The template-content textarea now sizes its `rows`
  automatically from the current content (`EditorRows` — minimum 6 rows, cap 30),
  so normal templates are visible without an internal scrollbar and short
  templates keep a sensible height. The fixed `min-height: 320px` was removed.
  No JavaScript was required; the change stays scoped to this page.
- **Preview mode.** Clicking **Preview** calls the existing
  `PreviewEmailTemplateQuery`; on success it enters preview mode, which hides the
  editor, Available placeholders, and the Save / Preview / Reset action group, and
  instead shows the rendered preview in a clearly identified preview surface
  ("Preview — not sent") plus a single **Edit template** button. The preview
  occupies the same content area as the editing workflow.
- **Edit template.** Clicking **Edit template** returns to edit mode, restoring
  the editor, placeholders, and actions while preserving the selected template and
  the current (possibly unsaved) editor content — it does not reload or discard
  edits.
- **Selecting another template.** Always returns to edit mode: it exits preview
  mode, clears the previous rendered preview and preview error/state, and loads
  the newly selected template's content, placeholders, and edit actions.
- **State management.** A new persistent `_isPreviewMode` field drives the UI
  mode, kept distinct from the existing `_isPreviewing` async generation flag.
  `_isPreviewing == true` means preview is currently being generated;
  `_isPreviewMode == true` means a generated preview is being displayed. The page
  does not enter preview mode when generation fails.
- **Error handling.** On preview failure the page remains in edit mode, keeps the
  editor/placeholders/actions, and shows the existing preview error. Save/reset
  error handling is unchanged.
- **Notification placement & single-message correction (final).** Save and reset
  notifications are now displayed only after the Available placeholders section and
  immediately before the action buttons (edit-mode order: Templates → editor →
  placeholders → notification → Save/Preview/Reset). At most one save/reset success
  notification is shown at any time: Save clears a prior reset message and Reset
  clears a prior save message, so two success banners are never rendered together.
  Notifications are not rendered in preview mode (preview mode shows only the
  selector, the rendered preview, and Edit template).
- **Fixed template set preserved.** Administrators still cannot create, rename, or
  delete templates. The underlying model, commands, queries, placeholder
  semantics, and business rules are unchanged.

No new UI framework, component architecture, autosave, sticky controls, tabs, or
unrelated navigation changes were introduced. The change is a presentation /
state-management refinement using the existing M12 visual language, Bootstrap, and
ATLAS conventions.

## Files changed

- `src/ATLAS.Blazor/Components/Pages/Admin/EmailTemplates.razor` — added mutually
  exclusive edit/preview modes (gated on `_isPreviewMode`), moved the action
  buttons to the bottom of edit mode, added the auto-sizing `rows="@EditorRows"`
  on the content textarea, and added the preview-mode layout with the `Edit
  template` control. Removed the fixed editor `min-height`. Added the preview
  error alert to edit mode so failures remain visible while staying in edit mode.
- `src/ATLAS.Blazor/Components/Pages/Admin/EmailTemplates.razor.cs` — added the
  persistent `_isPreviewMode` field, the `EditorRows` helper, the `EditTemplate()`
  method, and updated `SelectTemplate()` (always exit preview mode) and
  `PreviewTemplate()` (enter preview mode only on success). Final correction:
  `SaveTemplate()` clears any prior reset notification and `ResetTemplate()`
  clears any prior save notification so at most one success message is shown.
- `tests/ATLAS.Blazor.Tests/Components/Pages/Admin/EmailTemplatesPageTests.cs` —
  added focused tests for edit mode, preview mode, returning to edit mode,
  selecting another template from preview, and preview failure; added a second
  template to the test fixture.

## Tests

### Added or updated

Focused bUnit tests in `EmailTemplatesPageTests`:

- Edit mode:
  - `EditMode_SelectingTemplate_ShouldRenderEditor`
  - `EditMode_SelectingTemplate_ShouldRenderPlaceholdersAndActions`
  - `EditMode_Editor_ShouldHaveAutoSizingRowsAttribute`
- Preview mode:
  - `PreviewMode_ClickingPreview_ShouldCallPreviewQuery`
  - `PreviewMode_SuccessfulPreview_ShouldRenderPreviewAndHideEditor`
  - `PreviewMode_PreviewDoesNotRenderEditor`
- Returning to edit mode:
  - `ReturningToEditMode_EditTemplate_ShouldRestoreEditorAndPreserveContent`
- Selecting another template:
  - `SelectingAnotherTemplate_FromPreview_ShouldReturnToEditModeForNewTemplate`
- Preview failure:
  - `PreviewFailure_ShouldRemainInEditMode_AndShowError`
- Notification placement / single-message correction:
  - `SaveNotification_ShouldAppearAfterPlaceholders_AndBeforeActions`
  - `ResetNotification_ShouldAppearAfterPlaceholders_AndBeforeActions`
  - `Save_ShouldShowOnlySaveNotification`
  - `Reset_ShouldReplaceExistingSaveNotification`
  - `Save_ShouldReplaceExistingResetNotification`
  - `Notifications_ShouldNotRenderInPreviewMode`
- Existing tests (selection, reset, selector descriptions, placeholders table)
  were preserved and still pass.

### Executed

- `dotnet build ATLAS.slnx` — PASS (0 errors)
- `dotnet test ATLAS.slnx --no-build` — PASS (1131 tests across 6 projects)

## Validation

- Build — PASS: `dotnet build ATLAS.slnx` succeeded, 0 errors (no new
  compiler/analyzer errors).
- Focused Email Templates tests — PASS: 19 tests.
- Full automated suite — PASS: 1131 tests across 6 projects, all passed
  (ATLAS.Domain 183, ATLAS.API 55, ATLAS.Infrastructure 233, ATLAS.Application 280,
  ATLAS.Blazor 279, ATLAS.Integration 101).
- Component-level rendering — PASS: the mutually exclusive edit/preview modes,
  auto-sizing editor attribute, action placement, `Edit template` behavior,
  selection reset, and preview-failure behavior were verified through the Blazor
  component renderer (bUnit).
- Live visual check — ATTEMPTED but not fully completed: the app was launched and
  served, but `/admin/email-templates` redirects to interactive Microsoft Entra ID
  sign-in, which cannot be completed in this headless environment (see Known
  issues).

## Acceptance criteria

### Email Templates

- [x] Single-column workspace — templates selector, details/editor, placeholders,
      and actions form a coherent vertical workspace.
- [x] Desktop width — wider content area (max-width 1180px) retained.
- [x] Template selector — fixed list clearly selectable with name + description;
      selected state obvious and M12-consistent; no CRUD added.
- [x] Template details/editor — clearly separated; auto-sized content textarea is
      the dominant control; editing/validation/save preserved.
- [x] Placeholders — presented in a table with existing descriptions; behaviour
      unchanged.
- [x] Preview — distinct section using the existing rendered preview; behavioural
      change: it now replaces the editor (mutually exclusive mode).
- [x] Actions — Save (primary), Preview (secondary), Reset (destructive) as one
      coherent, compact, horizontally-aligned, responsive group at the bottom of
      edit mode.
- [x] **Edit/preview are mutually exclusive** — the page does not show the editor,
      placeholders, and rendered preview simultaneously; `Preview` replaces the
      editing workspace and `Edit template` restores it.
- [x] **Edit mode default** — selecting a template enters edit mode with editor,
      placeholders, and action buttons in order, actions at the bottom.
- [x] **Auto-sized editor** — textarea grows to show template content (min 6 rows,
      cap 30) without an internal scrollbar for normal content; no manual resize
      needed for normal templates; no fixed huge height.
- [x] **Preview mode** — existing `PreviewEmailTemplateQuery` used; on success
      enters preview mode, hides editor/placeholders/actions, shows the rendered
      preview in the same content area, marked as not sent.
- [x] **Edit template action** — restores edit mode preserving the selected
      template and current editor content (unsaved edits retained).
- [x] **Selecting another template** — always returns to edit mode, loads the new
      template's content, clears the previous preview and preview error/state.
- [x] **State management** — `_isPreviewing` (async operation) and `_isPreviewMode`
      (persistent UI mode) are distinct; preview mode is only entered on success.
- [x] **Error handling** — preview failure stays in edit mode, shows the existing
      preview error, keeps editor/placeholders/actions; save/reset handling
      unchanged.
- [x] **Notification placement (final)** — save/reset notifications appear after
      the Available placeholders section and immediately before the Save/Preview/
      Reset buttons (edit-mode order: Templates → editor → placeholders →
      notification → actions).
- [x] **Single save/reset notification** — at most one success notification is
      shown at any time: Save replaces a prior reset message and Reset replaces a
      prior save message; two success banners are never rendered together.
- [x] **Notifications hidden in preview mode** — preview mode shows only the
      selector, the rendered preview, and Edit template; save/reset notifications
      are not rendered.
- [x] **Existing functionality preserved** — selection, fixed list, descriptions,
      editing, placeholders + descriptions, Save, Reset, Preview, validation,
      error handling, authorization, route, commands/queries, placeholder
      semantics all unchanged.
- [x] **No unsupported additions** — no template CRUD, new types, autosave, sticky
      controls, tabs, new UI framework, or new component architecture.
- [x] **Responsive / accessibility** — no introduced page-level horizontal
      overflow; actions wrap; editor usable on narrow screens; selector and Edit
      template remain keyboard accessible; visible focus and button semantics
      intact (markup/CSS/component level).
- [x] **Regression safety / M12-009** — other accepted M12-009 and M12-009b areas
      (Permit Designer Preview, breadcrumbs, Users, Operations, Citizen
      Create/Edit) untouched; full suite passes.

## Documentation

- Updated `plans/tasks/reports/M12-009b-implementation-summary.md`.
- No architecture/shared-presentation change requiring additional documentation
  per `docs.instructions.md`; the change is a scoped presentation/state-management
  refinement. No other documentation changes required.

## Deviations

None from the task scope. The auto-sizing was implemented via a computed `rows`
attribute (pure Razor/Bootstrap) rather than JavaScript, which is smaller, needs no
new JS file or `App.razor` script registration, and fully satisfies the
"auto-size vertically based on content" requirement with a sensible minimum and
cap.

## Known issues / risks

- **Live browser validation not fully performed.** The `/admin/email-templates`
  page is protected by interactive Microsoft Entra ID sign-in, which cannot be
  completed in this headless environment. The app was launched and served (the
  page redirects to the Entra sign-in as expected). The mutually exclusive
  edit/preview behaviour, auto-sizing, action placement, `Edit template`, selection
  reset, and preview-failure behaviour were validated through genuine Blazor
  component rendering (bUnit) plus structural/markup review, but a true interactive
  browser pass (visual appearance at desktop/narrow/mobile, keyboard/focus, and
  contrast) was not executed. An authenticated live-browser review of the Email
  Templates page is recommended before milestone closure.

## Follow-up work

- Perform an authenticated live-browser review of the Admin Email Templates page
  at desktop, narrow desktop/tablet, and mobile widths: long content comfortable in
  the auto-sized editor, buttons at the bottom of edit mode, Preview replacing the
  editor (not below it), placeholders hidden in preview mode, `Edit template`
  restoring edit mode, and selecting another template returning to editing the
  newly selected template — including keyboard/focus and contrast checks.
