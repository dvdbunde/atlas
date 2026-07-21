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

---

# Phase A3: Complete Permit Type Designer

## Purpose
Complete the Designer with the Fields editor, Document Requirements editor, and a
Live Preview, reusing existing aggregate behaviors and the Citizen Portal dynamic-form
rendering infrastructure.

## Scope (In)
- **Permit Fields tab**: list, add, edit (inline editor), remove, reorder (up/down).
  Supports all `FieldType` values; Dropdown options captured one-per-line.
- **Document Requirements tab**: list, add, edit, remove, reorder with required flag,
  allowed extensions (comma-separated), and max file size.
- **Live Preview tab**: read-only rendering via `DynamicFormGenerator`
  (`FormFieldMode.ReadOnly`) driven by `FieldDefinitionDto` →
  `DynamicFormFieldViewModel.FromFieldDefinition`. No duplicated rendering logic.
- **Unsaved-changes across all sections**: single dirty flag guards section switching,
  in-app navigation, and browser unload. `_suppressUnsavedGuard` is set by
  Save/Cancel/Back-to-list so intentional navigation is not blocked (fixes A3.1 M2).
- **Entry points**: "Open Designer" on `PermitTypeDetail`, "Designer" link per row on
  the `PermitTypes` list (fixes A3.1 M3). Removed unused `_dataLoaded` field (M5).

## Scope (Out)
Draft/Publish, versioning, approval, collaborative editing, audit, cloning,
import/export, drag-and-drop.

## CQRS Alignment
- Reused: `UpdatePermitFieldCommand`, `RemovePermitFieldCommand`, `MovePermitFieldCommand`,
  `UpdateDocumentRequirementCommand`, `RemoveDocumentRequirementCommand`,
  `MoveDocumentRequirementCommand`, `GetPermitTypeByIdQuery`.
- **New (justified new interactions)**: `AddPermitFieldCommand`, `AddDocumentRequirementCommand`
  — the aggregate exposed `AddField`/`AddDocumentRequirement` but no command invoked them.
- `FieldDefinitionDto` gained `Id` (populated from `PermitField.Id`/`DocumentRequirement.Id`)
  so the UI maps rows to entities without positional heuristics.
- The UI never sets `Order` directly; reordering is delegated to the aggregate via
  `Move*` commands (bounds enforced by the domain).

## Tests
- Application: `AddPermitFieldCommandHandler`, `AddDocumentRequirementCommandHandler`
  (found + not-found) in `PermitTypeEditingCommandHandlerTests`.
- Blazor: field/requirement add/edit/remove/reorder, preview rendering, empty states,
  unsaved-changes guard (Cancel navigates without prompt), Designer entry point on Detail.
