# ATLAS Documentation Audit

**Repository snapshot:** uploaded `atlas.zip`
**Audit scope:** repository documentation, planning material, architecture/design records, ADRs, runbooks, engineering guidance, CI/CD references, and consistency with the source tree and project configuration.
**Audit date:** 15 September 2026

## 1. Executive summary

The repository has a substantial and generally well-organized documentation system. It contains a current root README, documentation indexes, architecture and design material, 24 ADRs, a product requirements document, engineering guidance, operational runbooks, infrastructure documentation, milestone plans, task specifications, and archived historical plans.

The principal issue is not an absence of documentation. It is **documentation drift between different levels of authority**:

- The root README presents the project as post-M12.
- The current-state architecture is explicitly post-M11 and still describes M11 validation as pending.
- The roadmap contains older milestone framing and historical assumptions that do not fully reflect the current post-M12 position.
- The environment setup runbook contains deployment steps and checklist references that appear to describe an earlier pipeline shape.
- Some documents correctly preserve historical decisions but are not always clearly separated from current operational guidance.
- At least one root README link points to a workflow file that is not present in the repository.
- Task files and reports use more than one report location convention.

**Overall assessment:** Documentation quality is structurally good but the repository needs a controlled “current-state refresh” rather than a wholesale rewrite.

### Recommended priority

1. Establish authoritative current-state documents and terminology.
2. Update `plans/ROADMAP.md` to reflect M12 completion and the undecided M13 position.
3. Reconcile `docs/architecture/current-state.md`, `docs/architecture/azure-infrastructure.md`, and the operations runbook with the actual deployed architecture and current milestone status.
4. Correct broken or stale links and workflow names.
5. Clarify historical versus normative documentation.
6. Add a concise application-workflow/status reference and a documentation maintenance policy.

## 2. Inventory

### Documentation groups found

| Area | Approximate contents | Assessment |
| --- | ---: | --- |
| Root project documentation | `README.md` | Current-looking, but contains at least one broken reference and should be verified against runtime/deployment details |
| Documentation index | `docs/README.md` | Useful index, but contains historical AI terminology and omits runbooks/notes |
| Architecture | `docs/architecture/*` | Valuable; current-state and infrastructure documents need reconciliation |
| Design | `docs/design/*` | Broad and useful; some documents describe extension points or proposals rather than current implementation |
| ADRs | 24 ADRs plus template/index | Strong decision history; needs status/implementation consistency review |
| PRDs | MVP PRD and templates | Useful baseline; should be labelled clearly as requirements/baseline, not current-state documentation |
| Engineering | standards/guidance, contract governance, PR guidelines, roadmap note | Useful, but some documents may reflect earlier process or workflow names |
| Runbooks | environment setup, operations | High-value operational documents; require current-state validation |
| Planning | roadmap, foundation plan, M12 plan, templates | Main source of milestone drift |
| Tasks/reports | M12 task specifications and implementation summaries | Detailed and valuable, but path conventions are inconsistent |
| Archive | superseded plans and final reviews | Appropriate conceptually; some historical files need clearer labels |
| AI/Copilot guidance | agents, chatmodes, instructions, prompts | Detailed and useful; chatmodes are explicitly deprecated but retained for history |

## 3. Source-of-truth recommendation

The repository should explicitly define the following hierarchy:

1. **Current implementation:** source code, project files, infrastructure code, and workflow files.
2. **Current product/application overview:** root `README.md`.
3. **Current architecture baseline:** `docs/architecture/current-state.md`.
4. **Current roadmap/status:** `plans/ROADMAP.md`.
5. **Normative technical decisions:** accepted ADRs, with implementation status noted where relevant.
6. **Operational procedures:** `docs/runbooks/`.
7. **Historical rationale and superseded plans:** `plans/archive/` and explicitly labelled historical documents.
8. **Milestone execution history:** milestone plans, task files, and implementation reports.

Add a short statement to `docs/README.md` explaining this hierarchy. Without it, readers may treat an old ADR, PRD, or milestone plan as a description of the current system.

## 4. Findings by priority

### P0 — Must correct before treating documentation as authoritative

#### P0.1 Broken workflow link in root README

The root README references:

`/.github/workflows/contract-validation.yml`

That file is not present in the uploaded repository. The available workflow files are:

- `01-package.yml`
- `02-deploy-dev.yml`
- `ci.yml`
- `dependency-review.yml`
- `docs-lint.yml`
- `policy-lint.yml`

**Impact:** Readers following the README cannot locate the referenced workflow.

**Recommendation:** Replace the link with the actual contract-validation location if contract validation is embedded in another workflow, or add/restore the intended workflow if it genuinely exists outside the ZIP snapshot. Verify all README links after correction.

#### P0.2 Current-state architecture is behind the root README

`docs/architecture/current-state.md` is titled **“Post M11”** and states that M11 has final Operations Workbook validation remaining. The root README identifies M12 as the most recently completed milestone and describes M12 changes.

**Impact:** Two documents can both appear authoritative while presenting different current project states.

**Recommendation:** Update the current-state architecture to a post-M12 baseline. Retain M9–M11 history in a dedicated “historical milestone context” section rather than in the document’s current-status statement.

#### P0.3 Roadmap status is not aligned with post-M12 reality

`plans/ROADMAP.md` contains older milestone framing and extensive A4–A7 administration-phase material. It also includes statements such as M1–M8 completion in July 2026 and describes a revised roadmap centered on older administration phases.

The repository now contains M12 plans and M12-011/M12-012 work, while M13 has not yet been selected.

**Impact:** The roadmap does not function as a reliable current navigation document.

**Recommendation:** Rewrite the top-level roadmap status section to state:

- Completed milestones through M12.
- What M12 delivered at a high level.
- M13: not yet selected; candidate areas are under evaluation.
- Older A4–A7 planning material either completed, superseded, or moved to an archive/reference section.

Do not delete the historical rationale until it has been preserved elsewhere.

### P1 — Important consistency and maintainability issues

#### P1.1 Environment setup runbook appears to contain an older deployment sequence

`docs/runbooks/environment-setup.md` describes remaining manual configuration, phase-based infrastructure work, and checklist items. It references workflows and stages including `03-smoke-tests.yml`, while the uploaded `.github/workflows/` directory contains no `03-smoke-tests.yml`.

The runbook also contains wording such as “remaining work” and “Phase 2” that may describe the original deployment rollout rather than the current repository state.

**Impact:** A developer or operator may follow steps for a workflow that no longer exists or misunderstand which steps are automated.

**Recommendation:** Split the document into:

- **Current deployment procedure** — verified against the current workflow files and Bicep.
- **One-time bootstrap/manual prerequisites** — only genuinely manual actions.
- **Historical deployment notes** — retained separately if useful.

Every referenced workflow filename should be checked against `.github/workflows/`.

#### P1.2 Operations runbook is explicitly M11-specific

`docs/runbooks/operations-runbook.md` repeatedly describes itself as based on M11 work and M11 live verification. This is valuable as a verification record, but it should not be the only current operations reference after M12.

**Recommendation:** Retain the M11 verification evidence, but add a clear banner:

> Historical verification baseline: this document records M11 validation. For current operational procedures and deployed-resource truth, consult the current operations section and infrastructure configuration.

Then update the current sections for the actual Grafana Cloud/Azure Monitor/Application Insights/Log Analytics arrangement.

#### P1.3 Azure infrastructure documentation mixes current architecture and legacy alternatives

`docs/architecture/azure-infrastructure.md` documents the current Azure foundation but also contains a legacy Azure Managed Grafana section and references remaining/future infrastructure work. ADR-024 separately states that Grafana Cloud is the final operational direction and that older Azure Managed Grafana modules are legacy and should be removed or reconciled.

**Impact:** Readers may not know which resources are intended to exist in the current deployment.

**Recommendation:** Divide the document into:

- Current deployed/managed resources
- Current deployment dependencies and manual prerequisites
- Deferred/future infrastructure
- Historical/legacy alternatives

Mark legacy material with a consistent `Historical / superseded` callout and link to ADR-024.

#### P1.4 Task report paths are inconsistent

Several M12 task files refer to reports under:

`plans/tasks/reports/`

Other current task documentation refers to:

`plans/reports/M12/`

The uploaded tree contains `plans/tasks/reports/task-implementation-summary-template.md`, but the current M12 reports may follow a different structure.

**Impact:** Contributors may create reports in inconsistent locations, making milestone history difficult to find.

**Recommendation:** Choose one convention. Suggested convention:

```text
plans/
└── reports/
    └── M12/
        └── M12-xxx-implementation-summary.md
```

Update `plans/tasks/README.md`, the task template, all active task files, and any agent instructions that mention the old path. Preserve redirects or notes if historical files already exist in the old location.

#### P1.5 `docs/README.md` is incomplete as a documentation index

The index lists Architecture, ADRs, Design, Engineering, and PRDs, but does not list:

- Runbooks
- Planning documents
- The roadmap
- Notes/backlog
- The OpenAPI contract
- Infrastructure documentation as a direct category

It also says the `docs/README.md` is displayed in preference to the root README. That statement is contextually useful but not particularly important to project users and distracts from navigation.

**Recommendation:** Make `docs/README.md` a proper documentation portal with sections for:

- Start here
- Architecture and design
- Product requirements
- Engineering and API governance
- Operations and deployment
- Planning and milestone history
- Decision records

#### P1.6 Root README has claims that should be explicitly verified against configuration

The root README lists .NET 9, Blazor Server, Azure SQL, Azure Blob Storage, Azurite, Azure Communication Services Email, Entra ID, App Service, Key Vault, ACR, Application Insights, Log Analytics, Docker, GitHub Actions, Bicep, MediatR/CQRS, and Clean Architecture/DDD.

The .NET 9 target is confirmed by the project files. The other items are broadly supported by the repository structure, but some are environment-dependent or may be optional in local development.

**Recommendation:** Keep the list, but distinguish:

- Core application technologies
- Azure deployment services
- Optional/local development services
- Operational/observability tooling

Avoid implying that every service is required for every local run.

### P2 — Recommended improvements

#### P2.1 Add a current application workflow reference

There is no obvious single concise document that defines the current application lifecycle and status transitions for all personas.

Create `docs/application-workflows.md` containing:

- Personas and permissions at a high level
- Status list
- Allowed transitions
- Assignment/release behavior
- Approve/reject/request-information behavior
- Citizen resubmission behavior
- Document handling
- Audit/activity expectations

This should be derived from the implementation and domain behavior, not from the original PRD alone.

#### P2.2 Add a current feature matrix

Create a compact matrix mapping major capabilities to personas and implementation status:

| Capability | Citizen | Officer | Admin | Status |
| --- | --- | --- | --- | --- |
| Draft applications | Yes | View where authorized | View where authorized | Implemented |
| Submission | Yes | — | — | Implemented |
| Assignment/release | — | Yes | As authorized | Implemented |
| Decisions | — | Yes | — | Implemented |
| Information request/resubmission | Respond | Request | View | Implemented |
| Document management | Upload/manage | Review/view | View where authorized | Implemented |
| Audit/activity | Relevant activity | Relevant activity | Audit viewer | Implemented |

The exact matrix should be verified against authorization policies and UI behavior before publication.

#### P2.3 Add documentation status metadata

For larger documents, add a small metadata block:

```markdown
> **Status:** Current | Historical | Proposed | Superseded
> **Last reviewed:** YYYY-MM-DD
> **Source of truth:** Code / Infrastructure / ADR / Product decision
```

This is especially useful for architecture, runbooks, PRDs, and roadmap documents.

#### P2.4 Clarify PRD versus current implementation

`docs/PRDs/atlas-mvp-prd.md` should be explicitly described as the MVP requirements baseline. It should not be expected to remain a complete description of the implemented application.

Add a note explaining that implementation may intentionally differ from the original PRD when superseded by accepted ADRs or later milestone decisions.

#### P2.5 Clarify ADR lifecycle

The ADR set is strong, but the index should explain whether ADRs are:

- Proposed
- Accepted
- Superseded
- Deprecated

ADR-024 already contains useful historical/supersession language. Apply the same convention consistently across all ADRs.

#### P2.6 Remove or isolate stale references to absent workflows

Search all documentation for workflow names and compare them with `.github/workflows/`. At minimum, investigate references to:

- `contract-validation.yml`
- `03-smoke-tests.yml`
- Any other workflow names not present in the repository

This should be a small dedicated cleanup task.

#### P2.7 Review external links and markdown quality

The repository has markdown linting and link-checking configuration, which is good. Run the actual configured checks against the complete documentation set. Pay particular attention to:

- Relative links after folder restructuring
- Links to archived files with unusual filenames
- URLs in historical documents
- Mermaid diagrams and Markdown rendering
- Duplicate headings and inconsistent title styles

## 5. Document-by-document recommendations

### Root `README.md`

**Status:** Mostly current; should be retained as the public project entry point.

**Keep:**

- Project description
- ATLAS expansion
- Persona-based capabilities
- Technology stack
- Architecture summary
- M12 status
- Build/test commands

**Update:**

- Broken contract-validation workflow link
- Add links to architecture, roadmap, setup, operations, and API contract documentation
- Clarify required versus optional local services
- Add repository status metadata or “last reviewed” date if desired
- Avoid making the README the detailed source for workflow semantics

### `docs/README.md`

**Status:** Needs navigation improvement.

**Update:** Convert into a complete documentation index and define the documentation source-of-truth hierarchy.

### `plans/ROADMAP.md`

**Status:** Highest-priority substantive update.

**Update:** Rebase around post-M12 reality; separate completed history, current status, and candidate future milestones. M13 should be marked undecided rather than represented by an old administration roadmap.

### `plans/atlas-foundation-plan.md`

**Status:** Likely historical/foundation reference.

**Recommendation:** Keep, but add a clear status banner identifying it as the foundation plan and not the current roadmap. Cross-link to the current roadmap.

### `plans/M12-plan.md`

**Status:** Current milestone plan/history.

**Recommendation:** Keep as the M12 scope document. Add a completion status and link to M12 implementation reports if not already present. Do not turn it into a general current-state document.

### `plans/tasks/M12/*`

**Status:** Valuable execution history.

**Recommendation:** Preserve task specifications. Mark completed tasks clearly and standardize report links. Check M12-012 original versus revised task naming and explain which specification is authoritative.

### `docs/architecture/current-state.md`

**Status:** Requires post-M12 refresh.

**Recommendation:** Make it the authoritative technical current-state document. Include actual project structure, runtime topology, persistence, identity, document storage, email, observability, deployment, and known limitations. Remove unresolved “pending” statements that are no longer true or explicitly date them as historical.

### `docs/architecture/azure-infrastructure.md`

**Status:** Useful but mixed current/legacy/future content.

**Recommendation:** Separate current resources from legacy Managed Grafana material and future evolution. Reconcile the text with `infra/main.bicep` and modules.

### `docs/runbooks/environment-setup.md`

**Status:** High operational value but likely stale in places.

**Recommendation:** Verify every command, workflow filename, role assignment, parameter name, resource name, and manual step against current scripts, Bicep, GitHub Actions, and app configuration.

### `docs/runbooks/operations-runbook.md`

**Status:** Valuable M11 verification record, not fully current by title/context.

**Recommendation:** Preserve evidence, add historical labeling, and create a short current operations procedure if necessary.

### `docs/PRDs/atlas-mvp-prd.md`

**Status:** Historical product baseline.

**Recommendation:** Add explicit “requirements baseline” labeling and links to later ADRs/milestones that supersede decisions.

### `docs/design/*`

**Status:** Generally useful, but mixed current design and future extension material.

**Recommendation:** Add status labels to proposal documents such as rescope proposals and discovery documents. Ensure diagrams match the current code structure.

### `docs/ADRs/*`

**Status:** Strong decision record collection.

**Recommendation:** Add consistent lifecycle metadata and perform a cross-check for ADRs whose implementation status has changed. In particular, reconcile ADR-019, ADR-023, and ADR-024 with current infrastructure and observability configuration.

### `.github/` documentation and instructions

**Status:** Detailed and valuable.

**Recommendation:** Keep the current instruction system intact. Review only for references to obsolete workflow names, old report paths, deprecated chat modes, or processes no longer used. The explicit retention of deprecated chatmodes is acceptable if clearly labelled as historical compatibility material.

## 6. Terminology findings

### ATLAS acronym

The root README currently defines ATLAS as:

>**Automated Tracking & Licensing Application System**

This should be treated as the canonical expansion unless another accepted project document explicitly establishes a different official form.

The alternative:

>**Application Tracking, Licensing And Services**

is not currently reflected in the root README and is less directly aligned with the documented permit-processing platform. It should not be introduced in parallel because two expansions create avoidable ambiguity.

### Recommended canonical terminology

- **ATLAS:** Automated Tracking & Licensing Application System
- **Product descriptor:** Case Management & Permit Processing Platform
- **Citizen:** End user submitting or managing an application
- **Officer:** Permit-processing/review user
- **Administrator:** Administrative/configuration/oversight user
- **Application statuses:** Use the exact enum/domain terminology in current code and document the display labels separately if they differ.

## 7. Suggested remediation plan

### Documentation maintenance milestone / short cleanup

1. Fix broken README workflow link.
2. Update `docs/README.md` index.
3. Refresh `plans/ROADMAP.md`.
4. Refresh `docs/architecture/current-state.md` to post-M12.
5. Reconcile Azure infrastructure documentation with Bicep and ADR-024.
6. Reconcile environment setup runbook with current workflows.
7. Mark M11 operations material as historical verification and create/update current operational guidance.
8. Standardize task report paths.
9. Add status metadata to architecture, runbook, PRD, proposal, and roadmap documents.
10. Add an application workflow/status reference.
11. Run documentation lint and link validation.
12. Review all references to absent workflow files.

### Suggested deliverables

- `docs/README.md` — revised index and source-of-truth guidance
- `docs/application-workflows.md` — current workflow/status reference
- `docs/architecture/current-state.md` — post-M12 current architecture
- `docs/runbooks/environment-setup.md` — verified current setup/deployment procedure
- `docs/runbooks/operations-runbook.md` — current operations plus historical M11 evidence
- `plans/ROADMAP.md` — post-M12 roadmap
- `plans/README.md` and `plans/tasks/README.md` — standardized planning/report conventions

## 8. What does not need wholesale rewriting

The following should generally be preserved rather than rewritten from scratch:

- ADRs, because they capture decision history and rationale.
- The MVP PRD, because it is a useful baseline if clearly labelled.
- Archived milestone plans and final reviews, because historical context is valuable.
- Detailed task specifications, because they provide implementation traceability.
- `.github` agent/instruction documents, provided their paths and process references remain valid.
- Design diagrams that still match the code and architecture.

The correct strategy is **current-state reconciliation plus targeted cleanup**, not deletion of historical material.

## 9. Final assessment

| Dimension | Assessment |
| --- | --- |
| Documentation breadth | Strong |
| Organization | Good, with some indexing gaps |
| Current-state accuracy | Mixed; root README is ahead of architecture/roadmap docs |
| Historical traceability | Strong |
| Operational readiness of documentation | Mixed; runbooks need verification |
| Terminology consistency | Mostly good; ATLAS expansion should be canonicalized |
| Link integrity | At least one confirmed broken reference; full link validation recommended |
| Maintenance conventions | Good foundations, but report paths and status metadata need standardization |

**Conclusion:** The repository has a solid documentation foundation. The immediate need is to establish a clearly labelled, post-M12 current-state layer and reconcile the roadmap, architecture, runbooks, and indexes with the actual repository. Once that is done, M13 planning can proceed from a reliable baseline.
