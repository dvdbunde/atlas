# Milestone 8 — Phase A3.1: Permit Type Designer Foundation

## Purpose
Establish the foundation of the Permit Type Designer: a shell that hosts future
configuration sections, a creation workflow that opens the designer, a General
Information editor, and unsaved-changes protection.

## Scope (In)
- Designer shell at `/admin/permit-types/{Id:guid}/designer` with section navigation
  (General active; Fields / Document Requirements / Preview as placeholders).
- Creation workflow at `/admin/permit-types/new` → `CreatePermitTypeCommand` →
  redirect to the new designer route.
- General Information editor (Name, Description) via `UpdatePermitTypeGeneralInformationCommand`.
- Save / Cancel / Back-to-list with unsaved-changes detection.

## Scope (Out)
Field editing, Document Requirement editing, Live Preview, Drag & Drop, Publish
workflow — all represented as `EmptyState` placeholders for later phases.

## Unsaved-Changes Strategy
- In-app navigation (section switching, back/forward) is guarded by
  `NavigationManager.RegisterLocationChangingHandler` with a `window.confirm` prompt.
- Browser refresh / close / back-button is guarded by a `beforeunload` handler
  (`wwwroot/js/unsavedChanges.js`) toggled via `IJSRuntime`.
- Dirty state is tracked in `PermitTypeDesignerViewModel.HasUnsavedChanges`.

## CQRS Alignment
A dedicated `UpdatePermitTypeGeneralInformationCommand` was introduced (rather than
extending the generic `UpdatePermitTypeCommand`) so the command model mirrors the
aggregate's evolving behavior and provides a clean foundation for subsequent
designer commands.

## Tests
- Domain: `PermitType.UpdateGeneralInformation` (valid + validation failures).
- Application: handler returns true on success, false when not found.
- Blazor: designer load/not-found/section nav/save/cancel/unsaved-changes; creation
  redirect + cancel.
