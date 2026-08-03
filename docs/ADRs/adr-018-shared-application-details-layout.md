---
title: "ADR-018: Shared Application Details Layout with RenderFragment Composition"
status: "Accepted"
date: "2026-07-22"
authors: "Engineering Team"
tags: ["architecture", "ui", "blazor", "composition", "milestone-8"]
supersedes: ""
superseded_by: ""
---

# ADR-018: Shared Application Details Layout with RenderFragment Composition

## Status

### Accepted (Implemented)

## Context

During Milestones 7 and 8, three distinct user personas required access to application details:

1. **Citizens** — View their own submitted applications; see a banner when an information request is pending, with an "Update and Resubmit" prompt
2. **Officers** — Review applications with workflow actions (Approve, Reject, Request Information); view application details alongside their review tools
3. **Administrators** — Browse all applications in read-only mode via the Application Explorer; no workflow actions permitted

Initially, each persona had (or would have) a separate page with duplicated layout, data-loading, and rendering logic. Inspection revealed early duplication patterns and the risk of divergence as more features were added.

Alternatives considered:

- **Separate pages per persona** — Simple but leads to code duplication and drift. Each page would reimplement the same layout, status badges, document lists, and timeline.
- **Single monolithic page with conditional blocks** — Centralizes layout but becomes hard to maintain as persona-specific content grows. `@if (IsOfficer)` / `@if (IsAdmin)` scattering leads to comprehension debt.
- **Base page class** — Sharing via inheritance. Fragile; changes to the base class affect all derived pages. Doesn't support different layouts well.

## Decision

Adopt a **shared layout component with RenderFragment extension points**:

1. Create a single `ApplicationDetailsLayout` component that owns the canonical application detail layout (header, status badges, officer assignment, form data, document list, activity timeline).
2. Define typed `RenderFragment` slots for persona-specific content:
   - `Actions` — header-level action buttons (e.g., "Assign to me")
   - `InfoRequestBanner` — citizen-only banner for pending information requests
   - `WorkflowActions` — officer-only approve/reject/request-info card
   - `AdditionalContent` — extensible slot for any role-specific sections
   - `DocumentSection` — replaces the default document table (officer uses a requirement-centric view; citizens see a flat document list)
3. Each consuming page (CitizenApplicationDetail, OfficerApplicationReview, AdminApplicationDetail) loads its own data and passes it into the shared layout.

### Composition Rules

- **Presentation only**: `ApplicationDetailsLayout` owns no data-loading logic. It receives all data as parameters. Data loading, MediatR calls, and error handling belong to the consuming page.
- **No role checks in the layout**: The layout does not check `AuthorizationState` or use `AuthorizeView`. Role-based rendering is achieved entirely through which `RenderFragment` slots the consuming page fills.
- **No workflow logic**: The layout does not implement approve/reject/request-info behavior. It renders whatever buttons consuming pages provide via slots.

## Consequences

### Positive

- Eliminates layout duplication across three persona pages
- New persona types (e.g., Supervisor) can be added by authoring a new consuming page and slotting content, without modifying the shared layout
- Activity Timeline replaced the earlier Lifecycle presentation approach — the timeline component (`ApplicationTimeline.razor`) is used uniformly across all personas
- Admin's Application Explorer (`/admin/applications`) and Application Detail share the same layout, providing a consistent read-only view
- UI composability improved — `RenderFragment` approach is idiomatic Blazor and well-understood by the team

### Negative

- Slightly more ceremony to set up a new consuming page (must wire all slots)
- If slot requirements grow significantly, the component interface may need versioning

## Implementation Notes

- `ApplicationDetailsLayout.razor` lives in `Components/Shared/ApplicationDetail/`
- Consuming pages: Citizen pages via `InfoRequestBanner` slot, Officer via `Actions`, `WorkflowActions`, and `DocumentSection` slots, Admin via no slots (pure read-only)
- The `ApplicationTimeline` component is used as a child within the layout, replacing the earlier ad-hoc lifecycle status display
- No new ADR is needed for the timeline decision — it is an implementation detail of the layout composition

## References

- ADR-001: Clean Architecture (layer separation enables clean component boundaries)
- ADR-005: Blazor Server (component model supports RenderFragment composition)
- Milestone 7 (Officer Review) and Milestone 8 (Administration) implementation
