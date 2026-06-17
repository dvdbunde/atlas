# Milestone 5 — UI Readiness Report

**Date:** 2026-06-17
**Status:** Complete
**Phase:** D8 (UI Hardening & Milestone Completion)

---

## Implemented Pages

| Page | Route | Status |
| ------ | ------- | -------- |
| PermitSelection | `/permits` | ✅ |
| ApplicationCreate | `/applications/create/{permitTypeId:guid}` | ✅ |
| ApplicationEdit | `/applications/edit/{id:guid}` | ✅ |
| ApplicationDetail | `/applications/{id:guid}` | ✅ |
| CitizenDashboard | `/dashboard` | ✅ |
| ConfirmationPage | `/applications/confirmation/{id:guid}` | ✅ |

All six pages route correctly through Blazor Router with InteractiveServer render mode. Authentication enforced via `[Authorize]` attribute on every page.

---

## Implemented Shared Components

| Component | Purpose | Consumed By |
| ----------- | --------- | ------------- |
| DynamicFormGenerator | Dynamic field rendering with validation. Supports Text, MultilineText, Number, Date, Boolean, Dropdown in Edit and ReadOnly modes. | ApplicationCreate, ApplicationEdit |
| DynamicFieldValidator | ValidationMessageStore-based required-field validation. No FluentValidation. | DynamicFormGenerator (internal) |
| StatusBadge | ApplicationStatus enum to contextual Bootstrap badge colors. | ApplicationDetail, CitizenDashboard, ConfirmationPage, ApplicationEdit |
| ApplicationTimeline | Chronological lifecycle milestones. Highlights current status, completed past states, skipped terminal states. | ApplicationDetail |

---

## View Models

| View Model | Purpose | Key State |
| ------------ | --------- | ----------- |
| PermitSelectionViewModel | Permit type card list | IsLoading, HasError, IsEmpty |
| ApplicationCreateViewModel | Draft creation flow | IsLoading, IsSaving, HasError, SaveSuccess |
| ApplicationEditViewModel | Draft editing + submission | IsLoading, IsSaving, SaveSuccess, IsSubmitting, SubmitHasError |
| CitizenDashboardViewModel | Application list with status-based nav | IsLoading, HasError, IsEmpty |
| ApplicationDetailViewModel | Detail display + timeline + reviews | IsLoading, HasError, IsLoaded, HasReviews |
| ConfirmationViewModel | Post-submit summary | IsLoading, HasError, IsLoaded |

All follow the same pattern: encapsulate page state, expose IsLoading/HasError/ErrorMessage, provide FromDto factory methods.

---

## End-to-End Citizen Journey

```text
PermitSelection (/permits)
    ↓  Apply
ApplicationCreate (/applications/create/{permitTypeId})
    ↓  Save Draft
ApplicationEdit (/applications/edit/{id})
    ↓  Submit Application
ConfirmationPage (/applications/confirmation/{id})
    ↓  View Application  or  Go to Dashboard
ApplicationDetail (/applications/{id})  or  CitizenDashboard (/dashboard)
```

No dead-end pages. Every state provides navigation to the dashboard or next logical step.

---

## Architecture

```text
Blazor Server (InteractiveServer)
    ↓  IMediator
Application Layer (Commands / Queries)
    ↓
Infrastructure (Repositories, DbContext)
```

Rules enforced:

- No HttpClient usage from Blazor
- No direct repository injection in Blazor components
- No DbContext references in UI layer
- No API endpoint calls from UI
- No FluentValidation in UI — uses ValidationMessageStore

---

## Application Layer Commands & Queries Consumed

| Direction | Command/Query | Used By |
| ----------- | -------------- | --------- |
| Read | GetActivePermitTypesQuery | PermitSelection |
| Read | GetPermitTypeByIdQuery | ApplicationCreate, ApplicationEdit, ApplicationDetail, ConfirmationPage |
| Read | GetApplicationByIdQuery | ApplicationEdit, ApplicationDetail, ConfirmationPage |
| Read | GetCitizenDashboardQuery | CitizenDashboard |
| Write | CreateDraftCommand | ApplicationCreate |
| Write | UpdateDraftCommand | ApplicationEdit |
| Write | SubmitDraftCommand | ApplicationEdit |

---

## Test Coverage

| Test Suite | Tests | Focus |
| ------------ | ------- | ------- |
| DynamicFormGeneratorTests | 21 | All 6 field types, validation, read-only, edit, accessibility, ordering |
| PermitSelectionTests | 10 | Loading, empty, error, cards, nav, accessibility, retry |
| ApplicationCreateTests | 11 | Loading, content, form, save, validation, error, saving state |
| ApplicationEditTests | 13 | Loading, pre-population, validation, save, submit, redirect, error |
| CitizenDashboardTests | 9 | Loading, content, status badges, empty, error, nav |
| ApplicationDetailTests | 9 | Loading, content, status, fields, timeline, reviews, error |
| ConfirmationPageTests | 7 | Loading, success, app number, nav, error |

**Total: 80 tests** across 7 test classes. All use bUnit with Moq for IMediator.

---

## UI States Matrix

| Page | Loading | Error | Empty | Success | Data |
| ------ | --------- | ------- | ------- | --------- | ------ |
| PermitSelection | ✅ Spinner | ✅ Danger + retry | ✅ Info alert | — | ✅ Cards |
| ApplicationCreate | ✅ Spinner | ✅ Danger + retry + dash link | — | ✅ Draft saved + edit link | ✅ Form |
| ApplicationEdit | ✅ Spinner | ✅ Danger + retry + dash link | — | ✅ Changes saved | ✅ Form + Submit |
| CitizenDashboard | ✅ Spinner | ✅ Danger + retry | ✅ Info + apply link | — | ✅ Table |
| ApplicationDetail | ✅ Spinner | ✅ Danger + retry + dash link | — | — | ✅ Detail + Timeline + Reviews |
| ConfirmationPage | ✅ Spinner | ✅ Danger + dash link | — | ✅ Summary + actions | ✅ Summary card |

---

## Status Consistency

All pages use ApplicationStatus enum — no magic strings, no integer comparisons.

| Component/Page | Status Usage |
| ---------------- | -------------- |
| StatusBadge | Switch on enum → Bootstrap badge |
| ApplicationTimeline | Enum → Completed/Current/Future/Skipped states |
| CitizenDashboard | Draft comparison for nav routing |
| ApplicationEdit | StatusBadge + draft guard on load |
| ApplicationDetail | StatusBadge in header |
| ConfirmationPage | StatusBadge in summary |

---

## Accessibility

- Loading spinners: `role="status"` with visible text
- Alerts: `role="alert"`
- Action buttons: `aria-label` describing the target
- Form labels: `for` attribute linked to input `id`
- Validation: `aria-describedby` on inputs, `.invalid-feedback` with `role="alert"`
- Navigation: standard `<a>` elements (keyboard accessible)
- Timeline: `role="list"`, `role="listitem"`, `aria-current="step"`

---

## Known Limitations

1. Dropdown options — no dynamic options from metadata. TODO M6.
2. File upload — not supported. TODO M6.
3. Boolean formatting — "true"/"false" in read-only, not "Yes"/"No".
4. Date formatting — raw strings, no locale-aware formatting.
5. CitizenNotes — hardcoded to empty string, not exposed on UI.
6. Field sort order — hardcoded to 0, uses DTO iteration order.
7. Label = FieldName — no separate display name concept.

---

## Deferred Items (M6+)

| Item | Priority |
| ------ | ---------- |
| File upload support | High |
| MultiSelect field type | Medium |
| Dropdown option configuration | Medium |
| Officer review dashboard | High |
| Admin permit type management | Medium |
| Email notification UI preferences | Low |
| Application cancellation | Low |
| Dashboard pagination | Low |
| Type-aware read-only formatting | Low |

---

## Open Risks

| Risk | Impact | Mitigation |
| ------ | -------- | ------------ |
| No SQL Server in local dev | Tests use InMemory DB | Use TestContainers for integration tests |
| Entra ID required for auth | Limited local dev | Dev mode uses AddAuthenticationCore |
| Seed data required for demo | Empty experience without it | SeedDataLoader runs on startup |
| Blazor Server circuit affinity | State lost on restart | Acceptable for MVP; sticky sessions configured |
| No monitoring in Blazor | Limited debugging | ILogger injected on all pages; Azure Monitor post-MVP |
