# Proposal: Re-Scope A6 — Reference Data & System Settings

**Status:** Proposal (for review)
**Date:** 2026-07-22
**Milestone:** 8 (Phase A6)
**Author:** ATLAS Developer agent (per inspection + maintainer direction)

## 1. Summary

The original A6 roadmap item ("Reference Data & System Settings") envisioned an
Administration Portal phase exposing application-managed reference data and
system settings. After a mandatory solution inspection and maintainer review, we
conclude that **A6 as originally envisioned should not be implemented**. There is
no concrete, ATLAS-owned business requirement justifying a new configuration or
reference-data subsystem, and introducing one would violate the explicit scope
constraints (no generic config framework, no key/value store).

This document recommends re-scoping A6 to a **documented no-op / architecture
conformance note**, and either reducing the milestone or merging the slot with A7.

## 2. Why A6 Should Be Re-Scoped

### 2.1 Inspection findings (no backing store exists)

The inspection confirmed the architecture has:

- **No** reference-data subsystem (no table, repository, or aggregate).
- **No** system-settings subsystem (no `SystemSetting` entity, no `I*SettingsRepository`).
- **No** generic configuration framework.
- Only deployment-time configuration: `appsettings.json`, `StorageOptions`
  (`IOptions<StorageOptions>`), `PermitTypes.json` seed, and compile-time enums.

Per the roadmap's own risk register (§5, row "Reference-data / system-settings
entities may not exist yet — A6 — Medium"), A6 was already flagged as carrying
scope risk because the backing entities did not exist. The inspection confirms
they still do not, and no requirement has emerged to create them.

### 2.2 What is explicitly NOT application configuration

The following were reviewed and must remain deployment-time / platform concerns.
Exposing any of them through administration would breach the scope constraints:

| Concern | Location | Reason to exclude |
|---------|----------|-------------------|
| `StorageOptions` (blob connection string, container, SAS) | `Infrastructure/Options/StorageOptions.cs` | Infrastructure + secret |
| Azure AD / Entra config | `appsettings.json` `AzureAd:*` | Authentication + secret |
| Connection strings | `appsettings.json` `ConnectionStrings:*` | Infrastructure + secret |
| Logging / `AllowedHosts` | `appsettings.json` | Telemetry / infra |
| `UserRole` enum | `Domain/Entities/User.cs` | Security model |
| `ApplicationStatus`, `ReviewDecision` enums | `Domain/Enums/*` | Workflow engine (domain model) |
| `AuditLogSortOption` enum | `Domain/Interfaces/AuditLogQueryOptions.cs` | Query parameter |
| `FieldType` enum | `Domain/Enums/FieldType.cs` | **Compile-time platform capability** — adding a type requires code, rendering, validation, persistence, and tests; not administrator-editable |
| `PermitTypes.json` | `Infrastructure/Data/SeedData/` | One-time deployment bootstrap |

### 2.3 No justified application-owned setting

A system setting is only warranted if ATLAS legitimately owns a runtime-editable
business default with no existing home. Candidates considered and rejected:

- Per-type limits (max file size, allowed extensions, required fields) already
  live inside `PermitType` / `DocumentRequirement` — owned per type, not global.
- Workflow states are fixed enums by design.
- No "maintenance mode", "global default fee", or similar requirement exists.

**Conclusion:** Forcing an aggregate into the model to satisfy the roadmap would
introduce a parallel configuration framework — the exact anti-pattern the scope
constraints forbid.

## 3. What (If Anything) Genuinely Belongs Under Application Configuration

**No currently identified application-owned runtime configuration exists.** The only items that *look* like configuration are
either:

1. Already owned per-aggregate (`PermitType`/`DocumentRequirement`), or
2. Deployment-time infrastructure that must stay out of administration.

If a concrete business requirement appears later (e.g., a system-wide default
that cannot live on an existing aggregate), it should be introduced as a single,
narrowly-scoped aggregate at that time — not preemptively.

## 4. Recommendation: Reduce, Merge, or Replace

| Option | Assessment |
| -------- | ------------ |
| **Reduce A6 to a documented no-op** | ✅ Recommended. Record A6 as "conformant — no new subsystem; architecture already separates deployment config from domain." Closes the milestone slot without wasted implementation. |
| **Merge A6 slot with A7** | Acceptable. A7 (Email Template Administration) has a real backing interface (`IEmailTemplateRenderer`) and a concrete requirement, so the "config-admin phase" slot is better spent there. Renumber so A7 becomes the final config-admin phase. |
| **Replace entirely** | Not needed; A7 already covers the remaining config-admin value. |

**Recommended path:** Mark A6 as **Reduced / Conformant** and let A7 absorb the
"config-admin phase" intent. No code changes for A6.

## 5. Conformance Statement (what A6 "delivers")

To satisfy the milestone without new code, A6 is recorded as delivering:

- Confirmation that deployment configuration (`StorageOptions`, Entra, connection
  strings, logging) is correctly excluded from the Administration Portal.
- Confirmation that `FieldType` and workflow enums remain platform/domain
  concerns, not administrator-editable.
- Confirmation that `PermitTypes.json` remains deployment-time bootstrap.
- A written boundary (this document) preventing future scope creep into generic
  configuration frameworks.

## 6. Next Steps

1. Maintainer approves this re-scope.
2. Update `milestone-08-revised-roadmap.md`: A6 → "Reduced / Conformant (no new
   subsystem)"; A7 becomes the final config-admin phase.
3. No implementation, no new aggregate, no migration.
4. Proceed to A7 (Email Template Administration) which has a justified backing
   interface.

## 7. Explicit Non-Goals

- No generic configuration editor.
- No arbitrary key/value storage.
- No infrastructure configuration exposure.
- No Azure / Entra administration.
- No secret management.
- No feature-flag framework.
- No new `SystemSetting` / `ReferenceData` aggregate until a concrete business
  requirement exists.
