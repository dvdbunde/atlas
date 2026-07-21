# Milestone 8 — Revised Roadmap (Remaining Phases)

## Context & Method

The original Milestone 8 plan was written before A1–A3 were implemented. Those phases delivered far more than "foundations": a **complete Administration Portal shell** (`PageHeader`, `EmptyState`, `AuthorizeView Roles="Admin"` nav, `GetAdminDashboardQuery` pattern), a **mature `PermitType` aggregate** (A2.5), and a **reusable configuration-editor pattern** (the Designer: tabbed sections, `Mediator.Send` command dispatch, unsaved-changes guard, `DynamicFormGenerator` reuse). Several assumptions behind the original remaining phases are now invalid, so this revision **reorganizes, simplifies, and rebalances** without adding features or expanding scope.

---

## 1. Revised Milestone 8 Roadmap

| Phase | Title | Status | One-line goal |
| ------- | ------- | -------- | --------------- |
| A1 | Administration Portal | ✅ Done | Portal shell, auth, nav, dashboard |
| A2 | Permit Type Administration | ✅ Done | List/detail/settings, deactivate |
| A2.5 | PermitType Aggregate Evolution | ✅ Done | Field/docreq editing operations |
| A3 | Complete Permit Type Designer | ✅ Done | Fields, Doc Reqs, Live Preview |
| **A4** | **Officer & User Administration** | 🟦 Revised | **Read-only** user directory (list + detail) per ADR-013; no role/activation writes |
| **A5** | **Audit Log Viewer** | 🟦 Simplified | Read-only viewer over existing `GetAuditLogsQuery` |
| **A6** | **Reference Data & System Settings** | 🟦 Merged | Single config-admin phase (was 2) |
| **A7** | **Email Template Administration** | 🟦 Revised | Admin UI over existing `IEmailTemplateRenderer` |

**Eliminated from original plan:**

- *"Dynamic Forms" admin page* — **duplicate** of the Permit Type Designer's Fields tab (the Designer already configures dynamic form fields per permit type). The `DynamicForms.razor` placeholder is retired.
- *Any standalone "Admin Portal infrastructure prep" phase* — **obsolete**; A1–A3 already built the shell, auth, and editor patterns those prep phases would have created.

---

## 2. Updated Phase Ordering

```txt
A1 → A2 → A2.5 → A3   (completed)
        ↓
       A4 → A5 → A6 → A7   (remaining, revised)
```

Rationale: A4 is the foundational "user administration" item from PRD M8 and unblocks role management; A5 is now a thin UI layer (backend exists); A6/A7 follow as independent config-admin phases reusing the same editor pattern.

---

## 3. Scope for Every Remaining Phase

### A4 — Officer & User Administration (READ-ONLY)

  > **Architectural constraint (ADR-013):** Microsoft Entra ID is the **sole source of truth** for user identity, profile, and role. The `User` aggregate is a synchronized, read-only projection. `User.SynchronizeFromClaims` overwrites `Role` on every login, and ADR-013 explicitly removed `ChangeRole`, `Deactivate`, `IsActive`, `UpdateUserRoleCommand`, and `CreateUserCommand`. **Therefore A4 is implemented as a read-only directory — no role editing, no activation/deactivation, no local writes to the `User` aggregate.**

- **In (implemented):** Read-only list of users (`/admin/users`) with search (name/email), role filter, sort, and paging via `GetUsersQuery`; read-only detail (`/admin/users/{id:guid}`) via `GetUserByIdQuery` showing profile, Entra-sourced role (badge), last login, and recent audit activity. Reuses Admin Portal shell + `PageHeader`/`EmptyState`.
- **Out (by architecture):** Role assignment/change, activate/deactivate, self-service registration, password reset, Entra ID provisioning UI (handled by Entra), citizen account management. These are intentionally **not** built to avoid the dual-write anti-pattern ADR-013 prohibits.
- **Implementation notes:** `GetUsersQuery`/`GetUserByIdQuery` handlers use the existing `IUserRepository` (`GetAllAsync`, `GetByIdAsync`) and `IAuditLogRepository` (`GetByUserIdAsync`). No new `User` aggregate methods were added. The UI surfaces an informational banner that identity/role are Entra-managed.

### A5 — Audit Log Viewer

- **In:** Read-only, filterable (user/action/date/entity) viewer over the **already-existing** `GetAuditLogsQuery` + `IAuditLogRepository` + `AuditLog` aggregate. Reuses `PageHeader`, list/table pattern, and dashboard nav entry.
- **Out:** Audit export, retention policy UI, log editing/deletion (immutable per ADR).

### A6 — Reference Data & System Settings

- **In:** Combine the two original placeholder pages (`ReferenceData`, `SystemSettings`) into **one** config-admin phase: edit lookup/reference values and operational settings through the same tabbed editor pattern established by the Designer.
- **Out:** New reference-data domains beyond what already exists; feature flags; integrations.

### A7 — Email Template Administration

- **In:** List/edit email templates through the existing `IEmailTemplateRenderer` contract; preview rendered output. Reuses the Designer's editor UX (label + textarea + save command).
- **Out:** Multi-channel/SMS (post-MVP per PRD §14); template versioning.

---

## 4. Dependencies Between Phases

| Phase | Depends on (already delivered) | New dependencies introduced |
| ------- | ------------------------------- | ------------------------------ |
| A4 | Admin Portal (A1), `User` aggregate, `GetByRoleAsync`, `IAuditLogRepository` | `GetUsersQuery` + `GetUserByIdQuery` (read-only); **no** role/activation commands (forbidden by ADR-013) |
| A5 | `GetAuditLogsQuery`, `IAuditLogRepository`, `AuditLog` | **None** (backend complete; UI + filters only) |
| A6 | Admin Portal shell, `PageHeader`/`EmptyState` | Reference-data/settings entities **if not already present** (scope risk — see §5) |
| A7 | `IEmailTemplateRenderer`, Admin Portal shell | Template persistence (store) **if not already present** (scope risk — see §5) |

A4 and A5 are **independent** of each other; A6/A7 are independent of A4/A5. Only A6/A7 carry a "may need a small domain entity" dependency.

---

## 5. Risks

| Risk | Phase | Likelihood | Mitigation |
| ------ | ------- | ----------- | ------------ |
| `User.SynchronizeFromClaims` overwrites `Role` on every login (claims are source of truth) | A4 | Resolved | **ADR-013 confirmed**: `User` is a read-only Entra projection; role/activation writes are removed. A4 is implemented read-only (list + detail) with no role/activation commands. Risk closed. |
| `GetAuditLogsQuery` returns **all rows** (no paging) — viewer will not scale | A5 | Medium | Add paging/sorting to the existing query as part of A5; backend is otherwise complete. |
| Reference-data / system-settings **entities may not exist** yet | A6 | Medium | Keep A6 to configuration that already has a backing store; defer any net-new reference domain to a later milestone. |
| Email template **persistence store** may not exist (only the renderer interface) | A7 | Medium | Confirm template storage in A7 discovery; if absent, scope A7 to renderer config only. |
| Over-merging A6 could blur two distinct config areas | A6 | Low | Keep `ReferenceData` and `SystemSettings` as separate *tabs* within one phase, not one merged entity model. |

---

## 6. Recommended Implementation Order

1. **A4 — Officer & User Administration** (foundational M8 item; highest product value; reuses most existing infra).
2. **A5 — Audit Log Viewer** (fastest win — backend already built; only UI + paging/filters).
3. **A6 — Reference Data & System Settings** (single config-admin phase; reuse Designer editor pattern).
4. **A7 — Email Template Administration** (reuse renderer + editor UX; lowest dependency).

Each phase is deliverable in a **single implementation cycle** (one feature branch, ≤400-line PR, full test coverage per Quality Policy).

---

## 7. Explanation of Every Change vs. Original Roadmap

| # | Original assumption | Change made | Why |
| --- | -------------------- | ------------ | ----- |
| 1 | A separate "Admin Portal infrastructure prep" phase was anticipated for each new admin area. | **Removed** — no prep phases remain. | A1–A3 already delivered the shell, auth, nav, `PageHeader`/`EmptyState`, and dashboard-query pattern. New admin pages are incremental, not foundational. |
| 2 | "Dynamic Forms" listed as a distinct admin capability. | **Eliminated** as duplicate. | The Permit Type Designer's Fields tab *is* the dynamic-form configuration per permit type. A standalone Dynamic Forms admin would duplicate it. |
| 3 | User admin and Officer admin treated as separate workstreams. | **Merged into A4** (Officer & User Administration). | Both operate on the single `User` aggregate (`Role`, `IsActive`); one phase, one editor pattern. |
| 4 | Reference Data and System Settings as two separate phases. | **Merged into A6** (one config-admin phase, two tabs). | Both are simple lookup/settings CRUD reusing the same list/detail/editor UX; separating them adds overhead without architectural benefit. |
| 5 | Audit Log Viewer assumed to need backend build-out. | **Simplified to UI-only (A5)**. | `GetAuditLogsQuery`, `IAuditLogRepository`, and `AuditLog` aggregate already exist; only the viewer UI + paging/filters remain. |
| 6 | Email Templates assumed to need a from-scratch service. | **Revised to admin-UI-over-existing-renderer (A7)**. | `IEmailTemplateRenderer` already exists; A7 is configuration UI, not service design. |
| 7 | Phases ordered by original discovery sequence. | **Reordered A4 → A5 → A6 → A7** by dependency + value. | A4 delivers the core M8 "user administration" goal; A5 is a cheap backend-complete win; A6/A7 are independent config phases. |
| 8 | Each phase expected to introduce its own editor UX. | **Standardized on Designer patterns** (tabbed sections, `Mediator.Send` commands, unsaved-guard, `DynamicFormGenerator` reuse). | Eliminates duplicated UX effort and keeps admin modules consistent. |

---

**Net effect:** The revised roadmap removes ~2 redundant/duplicate workstreams (Dynamic Forms, infra-prep phases), merges 2 pairs of phases (User+Officer, ReferenceData+SystemSettings), downgrades Audit Viewer to a UI-only phase, and reuses the A1–A3 Administration Portal + Designer infrastructure across all four remaining phases — keeping each deliverable to a single implementation cycle with no scope expansion.
