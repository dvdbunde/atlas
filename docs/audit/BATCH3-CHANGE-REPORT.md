# Batch 3 Change Report

## Scope

Updated the environment and operations runbooks while preserving their existing instructions, queries, checklists, and historical evidence wherever possible.

## Files changed

- `docs/runbooks/environment-setup.md`
- `docs/runbooks/operations-runbook.md`

## Corrections

### environment-setup.md

- Corrected the deployment workflow sequence to match the workflows present in the repository:
  - `.github/workflows/ci.yml`
  - `.github/workflows/01-package.yml`
  - `.github/workflows/02-deploy-dev.yml`
- Removed references that implied a `03-smoke-tests.yml` workflow exists.
- Replaced the checklist item for that nonexistent workflow with a generic deployment-validation check.
- Preserved the existing manual smoke-test and validation instructions.

### operations-runbook.md

- Removed the title's fixed “Milestone 11” designation so the document can serve as the current operational reference.
- Clarified that M11 live-verification statements are historical evidence and should be revalidated after relevant changes.
- Softened claims about the Operations Workbook's validation status where the repository alone cannot prove current live status.
- Preserved the existing operational queries, metrics, alert descriptions, troubleshooting steps, and historical verification details.

## Preservation statement

No substantive operational procedure, query, checklist section, metric name, alert description, or historical verification detail was intentionally removed or condensed.

## Not changed

- Application code
- Infrastructure code
- GitHub Actions workflows
- Azure resources
- Alert thresholds
- Deployment behavior
