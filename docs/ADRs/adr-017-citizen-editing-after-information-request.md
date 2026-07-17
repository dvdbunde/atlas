# ADR-017: Citizen Editing After Information Request

## Status

Accepted

## Context

Phase O5 introduces the citizen response workflow after an Officer requests additional information. A design question arose: should the citizen be allowed to edit all application data, or only the specific items the Officer requested?

## Decision

The citizen may edit all dynamic field values and supporting documents when responding to an information request. Immutable metadata (application reference, submission timestamps, audit history, assignment) remains protected by the aggregate.

## Rationale

- The aggregate already supports full editing for `InfoRequested` status.
- Government permit processing systems universally allow full application editing during information-request cycles.
- Field-level locking adds significant complexity for no business benefit.
- Future change-highlighting (Option C) can be layered on top without restricting what the citizen edits today.

## Consequences

- Citizens have a familiar, flexible editing experience.
- Officers review the complete resubmitted application, not just changed fields.
- No new aggregate state, persistence, or UI gating is required.
- Future milestones may introduce change-tracking without altering the editing model.

## Alternatives Considered

- **Option B (Requested Items Only)**: Rejected due to high complexity, poor UX, and lack of alignment with government permit processing standards.
- **Option C (Mixed Model)**: Deferred. Full editing now; change-highlighting belongs in O6/O7 timeline/history work.

## References

- ADR-004: Domain-Driven Design
- ADR-002: CQRS with MediatR
- Phase O5 implementation (UpdateDraftCommand status guard, ApplicationEditViewModel, ApplicationEdit.razor)
