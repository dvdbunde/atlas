# M12 --- ATLAS UI Restyling

> **TL;DR:** M12 restyles the existing ATLAS Blazor UI into a
> professional, restrained and consistent visual system. It deliberately
> preserves existing pages, components, workflows and functionality;
> implementation is CSS-first with only small targeted markup changes.

## 1. Title (string, max 120 chars)

M12 --- ATLAS Blazor UI Restyling

## 2. Short description (string, 1-3 sentences, max 500 chars)

Restyle the existing ATLAS Blazor application to provide a professional,
minimal, consistent visual language across Citizen, Officer, and Admin
areas. Preserve existing pages, shared components, workflows,
information architecture, and functionality; use CSS and small targeted
markup adjustments rather than rebuilding the UI.

## 3. Current status (object)

``` yaml
owner: David Van den Bunder <N/A> — Product Owner
state: proposed
last_updated: 2026-09-02
blockers: []
```

## 4. Objectives (ordered list, 1-10 items)

1. Replace the default Blazor/Bootstrap visual appearance with a
    coherent ATLAS visual language.
2. Establish reusable global styling tokens for colour, typography,
    spacing, surfaces, controls, and semantic states.
3. Restyle the existing application shell and role-based navigation
    without changing its functional structure.
4. Restyle existing shared Blazor components so common UI patterns have
    a consistent appearance.
5. Apply the approved visual treatment to the existing Citizen,
    Officer, and Admin pages.
6. Preserve all existing functionality and information architecture
    while allowing small layout adjustments that improve hierarchy,
    density, readability, and responsiveness.
7. Validate the resulting UI for visual consistency, responsive
    behaviour, keyboard focus, and colour contrast.
8. Refactor the existing Admin Dashboard into a more informative
    administrative overview and navigation hub without introducing new
    application functionality.

## 5. Success criteria (list; each item must be measurable and include acceptance criteria)

- name: Visual consistency metric: Existing ATLAS pages using the M12
    visual language target: 100% of in-scope Blazor pages verification:
    Review all Citizen, Officer, Admin, shared, shell, error, and
    authentication-related UI surfaces against the M12 specification.

- name: Functional preservation metric: Existing user workflows
    affected by M12 target: No intentional functional changes
    verification: Run the existing automated test suite and manually
    exercise representative Citizen, Officer, and Admin workflows.

- name: Shared styling metric: Existing shared UI components using the
    common M12 styling patterns target: 100% verification: Inspect
    shared components and rendered pages for consistent tokens,
    controls, cards, badges, tables, and spacing.

- name: Accessibility metric: In-scope interactive UI with visible
    keyboard focus and sufficient contrast target: 100% of reviewed
    controls verification: Keyboard walkthrough plus automated/manual
    accessibility checks available in the project.

- name: Responsive presentation metric: Representative Citizen,
    Officer, and Admin pages usable at supported viewport sizes target:
    No new horizontal overflow or inaccessible controls introduced
    verification: Test representative pages at desktop, tablet, and
    narrow/mobile widths.

## 6. Scope (object with two arrays: in, out)

### In

- Restyle `src/ATLAS.Blazor/wwwroot/app.css`.
- Restyle `MainLayout`, `NavMenu`, and their existing scoped CSS.
- Restyle existing shared components and their existing markup where
    practical.
- Restyle existing Citizen pages.
- Restyle existing Officer pages.
- Restyle existing Admin pages.
- Refactor the existing Admin Dashboard as specified by M12-008,
    including informative summary metrics and navigation to existing
    Admin pages.
- Apply the approved ATLAS colour, typography, spacing, surface,
    control, table, badge, and responsive conventions.
- Keep Bootstrap as the underlying CSS/layout framework unless a
    specific existing Bootstrap behaviour materially blocks the approved
    visual treatment.
- Make small targeted markup/layout adjustments only where needed to
    support the visual treatment.
- Validate the six representative screens used for design review:
    Citizen Dashboard, Citizen Permit Creation, Officer Dashboard,
    Officer Review, Admin Dashboard, and Admin Operations.

### Out

- Rebuilding existing Razor pages from scratch.
- Replacing existing shared components with a new component
    architecture.
- Introducing a new UI component framework.
- Introducing a new frontend styling framework.
- Changing business logic, domain models, APIs, persistence,
    authentication, or authorization.
- Changing user workflows or information architecture.
- Adding new dashboard functionality beyond the approved M12-008
    administrative summary metrics and navigation links to existing
    pages.
- Reworking components solely to make mockups resemble a different
    application.
- Replacing Bootstrap as a separate project.

## 7. Stakeholders & Roles (table or list)

| Stakeholder | Role | Responsibility |
| --- | --- | --- |
| David Van den Bunder | Product Owner | Approve visual direction, scope, and final result. |
| Implementation Agent / Developer | Implementer | Apply M12 styling according to the plan, task files, and repository instructions. |
| Code Reviewer | Reviewer | Verify correctness, maintainability, accessibility, and adherence to M12 constraints. |
| Tester | Tester | Validate existing behaviour and M12 UI/responsive acceptance criteria. |

## 8. High-level timeline & milestones (ordered list)

Dates are `N/A` because an implementation schedule has not yet been
approved. Email addresses are `N/A` because no project contact addresses
were supplied; they must not be invented.

1. `M12-A — Global visual foundation complete — N/A — Implementation Agent <N/A>`
2. `M12-B — Application shell restyled — N/A — Implementation Agent <N/A>`
3. `M12-C — Shared components restyled — N/A — Implementation Agent <N/A>`
4. `M12-D — Citizen pages restyled — N/A — Implementation Agent <N/A>`
5. `M12-E — Officer pages restyled — N/A — Implementation Agent <N/A>`
6. `M12-F — Admin pages restyled — N/A — Implementation Agent <N/A>`
7. `M12-G — Consistency/accessibility validation complete — N/A — Tester / Code Reviewer <N/A>`
8. `M12-H — Admin Dashboard refactor complete — N/A — Implementation Agent <N/A>`
9. `M12-I — Blazor page navigation and UX improvements complete — N/A — Implementation Agent <N/A>`
10. `M12-J — Milestone review and approval — N/A — David Van den Bunder <N/A>`

## 9. Task list (hierarchical, actionable tasks with complexity estimates)

- T-001 \| Establish global ATLAS visual foundation and design tokens
    \| Implementation Agent \| complexity: M \| deps: \[\] \| done:
    false
- T-002 \| Restyle the existing application shell and role-based
    navigation \| Implementation Agent \| complexity: M \| deps:
    \[T-001\] \| done: false
- T-003 \| Restyle existing shared UI components \| Implementation
    Agent \| complexity: L \| deps: \[T-001\] \| done: false
- T-004 \| Restyle existing Citizen pages \| Implementation Agent \|
    complexity: M \| deps: \[T-002, T-003\] \| done: false
- T-005 \| Restyle existing Officer pages \| Implementation Agent \|
    complexity: M \| deps: \[T-002, T-003\] \| done: false
- T-006 \| Restyle existing Admin pages \| Implementation Agent \|
    complexity: L \| deps: \[T-002, T-003\] \| done: false
- T-007 \| Perform cross-application consistency, responsive, and
    accessibility pass \| Tester / Implementation Agent \| complexity: M
    \| deps: \[T-004, T-005, T-006\] \| done: false
- T-008 \| Refactor the existing Admin Dashboard into an informative
    administrative overview and navigation hub \| Implementation Agent
    \| complexity: M \| deps: \[T-006\] \| done: false
- T-009 \| Refactor and improve Citizen, Officer, and Admin Blazor page
    navigation and UX \| Implementation Agent \| complexity: L \| deps:
    \[T-007, T-008\] \| done: false

## 10. Risks and mitigations (table or list)

- R-001: Styling work expands into page/component redesign \|
    probability: medium \| impact: high \| mitigation: Treat the
    explicit M12 scope boundary as a hard constraint. Prefer CSS-first
    changes and require justification for markup restructuring. \|
    owner: Product Owner
- R-002: Bootstrap overrides become fragmented or page-specific \|
    probability: medium \| impact: medium \| mitigation: Establish
    global tokens and shared styling patterns first; avoid one-off CSS
    unless a page genuinely has a unique requirement. \| owner:
    Implementation Agent
- R-003: Visual improvements unintentionally alter existing behaviour
    \| probability: low \| impact: high \| mitigation: Keep markup
    changes minimal and validate representative workflows plus the
    existing automated test suite. \| owner: Tester
- R-004: Accessibility regresses during visual changes \| probability:
    medium \| impact: high \| mitigation: Preserve semantic controls,
    maintain visible focus and contrast, and include accessibility
    validation in M12-007. \| owner: Tester
- R-005: Large-screen layouts remain too sparse or small-screen
    layouts become cramped \| probability: medium \| impact: medium \|
    mitigation: Validate representative pages at multiple viewport sizes
    and tune container widths/spacing as part of M12-007. \| owner:
    Implementation Agent

## 11. Assumptions (list)

- The current ATLAS Blazor pages and shared components remain the
    functional source of truth.
- Bootstrap remains available and can be overridden through the ATLAS
    styling layer.
- The six supplied design-reference screens are visual direction only,
    not a specification for new functionality.
- Existing `.github` instructions and agents remain authoritative for
    engineering workflow, testing, review, and coding standards.
- M12 does not require a new backend or API change.
- Exact colour values may be tuned during implementation while
    preserving the approved visual character and restrained palette.

## 12. Implementation approach / Technical narrative (detailed, up to 1000-2000 words)

M12 should be implemented from the outside inward.

First, establish the global ATLAS visual foundation in `app.css`: colour
tokens, typography, spacing, surfaces, borders, focus treatment, control
defaults, and semantic states. The foundation should override or
complement Bootstrap rather than replacing Bootstrap wholesale.

Second, apply the foundation to the existing application shell.
`MainLayout.razor`, `MainLayout.razor.css`, `NavMenu.razor`, and
`NavMenu.razor.css` should retain their existing functional structure
and role-based navigation. The current gradient sidebar should be
replaced with the approved restrained dark-navy treatment, and the
existing top row should become a cleaner white application header.

Third, restyle the existing shared components. Components such as
`StatusBadge`, `ApplicationSummaryCard`, `ApplicationTimeline`,
`ApplicationActivityFeed`, `DocumentRequirementCard`, `PageHeader`,
`EmptyState`, `ApplicationDetailsLayout`, and `DynamicFormGenerator`
should remain existing components. The preferred change is styling and
small markup adjustments only where necessary.

Fourth, apply the common visual treatment to Citizen, Officer, and Admin
pages. The role-specific distinction is primarily density and emphasis,
not separate design systems. Citizen pages should remain calmer and
simpler; Officer pages can be information-dense; Admin pages can be
operational and table-oriented. None of these differences justify
rebuilding the page structures.

The existing Admin Dashboard is then refactored as a focused exception
to the otherwise styling-first approach. M12-008 may make targeted
markup and data-query changes because the approved outcome is to make
the dashboard more informative and useful, while retaining its role as
an overview of existing Admin areas. The dashboard should contain six
navigational summary blocks: Permit Types, Applications, Users, Audit
Logs, Email Templates, and Operations. Each block links to its
corresponding existing Admin page.

Permit Types should show the total in the block title and
active/inactive counts in the body. Applications should show the total
in the title and only non-zero counts for the defined Draft, Submitted,
Under Review, Info Requested, Resubmitted, Approved, and Rejected
statuses. Users should replace the separate Citizen, Officer, and Admin
counters and show a total plus the Citizen, Officer, and Administrator
breakdown. Audit Logs should summarize recent activity, preferably using
an existing last-24-hours event count and latest-event recency where the
current data/services support those metrics. Email Templates should
retain a simple total count because the existing functionality has no
active/inactive distinction. Operations should provide a lightweight
system-health summary and reuse the existing Operations health
determination rather than introducing a separate health calculation.

These dashboard changes must not introduce new pages, workflows,
authorization rules, or unrelated analytics. Existing services and query
infrastructure should be reused wherever practical.

M12-009 extends the same preservation principle to targeted page-level
navigation and UX improvements. Breadcrumbs are the canonical hierarchical
navigation mechanism throughout the Citizen, Officer, and Admin areas;
redundant Back buttons used solely for parent/up navigation should be
removed. The existing hierarchy and routes remain authoritative.

The Admin Users role indicator should preferably use plain role text. If
visual differentiation is retained, the three roles must have distinct,
accessible treatments using the existing M12 visual system.

The Admin Email Templates page should use the approved single-column flow:
fixed template list, selected template content, available placeholders,
preview, and the existing Save, Preview, and Reset to default actions.
No template creation functionality is introduced.

The Operations Deeper Telemetry block should link to the Azure Portal for
Azure resources that can be inspected there and to the ATLAS Grafana Cloud
dashboards. Azure Managed Grafana must not be referenced. The Grafana
dashboard URL should come from Blazor application configuration and is a
non-secret value.

The Permit Designer Preview should present a realistic application-style
preview using the existing application-detail presentation where practical,
with clearly identified dummy/preview data. Preview data must remain
transient and must never create or persist a real application.

Citizen Application Create/Edit should maintain one current save-result
notification: a later successful save replaces the earlier draft-created
message, and redundant Continue Editing actions are removed. Save Changes
and Submit Application should use the common M12 button treatment and form
a compact horizontal action group where space permits. Application data and
supporting documents should be visually distinct while retaining all
existing fields, validation, upload, save, and submit behaviour.

The implementation should avoid invented UI. The supplied mockups
demonstrate a visual direction, but existing page content and
functionality are authoritative. If a mockup shows an element that does
not exist in ATLAS, that element must not be added merely to match the
mockup.

The preferred technical strategy is CSS-first. Use shared classes, CSS
custom properties, and existing Bootstrap utilities where they reduce
duplication. Avoid inline styling. If a component's markup prevents a
required visual or responsive treatment, make the smallest targeted
change that solves the problem and preserve its existing behaviour.

Validation should compare the rendered application against the approved
design specification and the original screenshots. Representative pages
should be checked after each major task rather than waiting until the
final pass. Existing automated tests remain the primary regression gate
for behaviour; visual and accessibility checks supplement them.

No data migration, deployment architecture change, API change, or
rollback-specific infrastructure is required. M12 is a frontend
presentation change and can be reverted through the normal
source-control workflow if a regression is identified.

For each completed M12 task, the implementation agent must create an
implementation summary using
`plans/tasks/reports/task-implementation-summary-template.md` and save
it as `plans/tasks/reports/[TASK-ID]-implementation-summary.md`.

## 13. Testing & validation plan

### Unit / component tests

- Run the existing Blazor/component test suite.
- Do not add tests solely for static CSS unless the project already
    has an established visual/component testing mechanism.
- If markup changes affect an existing tested component, update the
    relevant tests as required by repository instructions.

### Integration tests

- Run the existing application/integration test suite.
- Confirm authentication and role-based navigation still render the
    same functional destinations.

### End-to-end / UI validation

Validate at minimum:

- Citizen Dashboard
- Citizen Permit Creation
- Officer Dashboard
- Officer Review
- Admin Dashboard
- Admin Operations

For each, verify that existing controls, links, forms, tables, status
indicators, and actions remain available and functional.

### Responsive validation

Check representative pages at desktop, tablet, and narrow/mobile widths.
Confirm that navigation, forms, tables, actions, and content remain
usable and that no new unintended horizontal overflow is introduced.

### Accessibility validation

- Keyboard navigation.
- Visible focus.
- Contrast of text, controls, borders, and semantic states.
- Labels and semantic structure where markup is changed.
- Status information is not communicated by colour alone.

## 14. Deployment plan & roll-back strategy

### Environments

Use the existing ATLAS development/test/staging/production deployment
process.

### Deployment

1. Implement tasks in dependency order.
2. Run relevant automated tests after each task.
3. Complete M12-007 validation.
4. Complete normal code review and CI checks.
5. Deploy using the existing pipeline.

### Rollback

If a visual or functional regression is discovered, revert the affected
M12 change through the normal Git workflow and redeploy using the
existing pipeline. No special database or infrastructure rollback is
required.

## 15. Monitoring & observability

No new application telemetry is required for M12.

Validate that the styling changes do not interfere with existing
application telemetry, error boundaries, navigation, or operational UI.

## 16. Compliance, security & privacy considerations

- No new personal data is introduced.
- No authentication or authorization behaviour should change.
- Preserve existing secure form/control behaviour.
- Maintain accessibility requirements.
- Do not introduce external assets or services unless explicitly
    approved and required.

## 17. Communication plan

M12 task completion should be communicated through the existing
repository workflow and pull request process. The implementation report
should identify the tasks completed, files/areas changed, tests run,
visual validation performed, and any deviations from the approved
specification.

## 18. Related documents & links

- `plans/tasks/M12/M12-001.md` through `plans/tasks/M12/M12-010.md`
- `plans/reports/task-implementation-summary-template.md` ---
    required format for M12 task implementation summaries.
- Existing `.github` instructions and agents
- Supplied M12 screenshot reference set
- Approved M12 UI Design Specification from the milestone planning
    discussion

## 19. Appendix (examples, data samples, migration mappings)

### Approved visual direction summary

- Solid dark-navy application sidebar; remove the current blue/purple
    gradient.
- Light neutral page background with white content surfaces.
- Restrained ATLAS blue accent for primary actions, links, and
    selected states.
- Separate semantic success, warning, error, and information colours.
- Modern sans-serif typography with clear hierarchy.
- 8px-based spacing scale.
- Subtle borders and restrained shadows.
- Existing cards, tables, forms, buttons, badges, and page headers
    restyled rather than replaced.
- Citizen, Officer, and Admin areas share one visual system with
    different information density.
- No invented functionality.
