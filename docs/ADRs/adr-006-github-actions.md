---
title: "ADR-006: Use GitHub Actions for CI/CD"
status: "Accepted"
date: "2026-06-03"
authors: "David (Product Owner), Engineering Team"
tags: ["ci/cd", "github-actions", "devops"]
supersedes: ""
superseded_by: ""
---

# ADR-006: Use GitHub Actions for CI/CD

## Status

### Accepted

## Context

ATLAS is hosted on GitHub (dvdbunde/atlas) and requires automated build, test, and deployment pipelines. We need a CI/CD solution that:

1. **Integrates with GitHub** - Single platform for source control and pipelines (reduce tool sprawl)
2. **Supports .NET 9** - Build and test C# projects (PRD C-01)
3. **Deploys to Azure** - App Service, SQL Database, Blob Storage (ADR-003, ADR-007)
4. **Enforces quality gates** - Run unit tests, integration tests, code coverage (PRD Quality Policy)
5. **Manages infrastructure** - Deploy Bicep templates (ADR-007) as part of pipeline
6. **Supports PR workflow** - Validate changes before merge (PRD branch/PR naming conventions)

Alternative CI/CD platforms considered:

- **Azure DevOps Pipelines** - Powerful but separate from GitHub, requires additional licenses
- **Jenkins** - Self-hosted, high maintenance overhead, not cloud-native
- **GitLab CI** - Excellent CI/CD but would require migrating repository from GitHub
- **CircleCI** - Cloud-native but separate platform, additional cost for private repos

## Decision

We will use **GitHub Actions** as the CI/CD platform for ATLAS.

### Pipeline Architecture

```text
┌─────────────────────────────────────────────────┐
│              GitHub Repository                  │
│                   (main branch)                 │
└──────────────┬──────────────────────────────────┘
               │ Push or PR
               │
┌──────────────▼───────────────────────────────────┐
│         GitHub Actions Workflows                 │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │  ci.yml - Continuous Integration           │  │
│  │  Triggers: PR to main, push to main        │  │
│  │                                            │  │
│  │  1. Checkout code                          │  │
│  │  2. Setup .NET 9 SDK                       │  │
│  │  3. Restore dependencies                   │  │
│  │  4. Build solution                         │  │
│  │  5. Run unit tests (Atlas.Domain.Tests)    │  │
│  │  6. Run integration tests                  │  │
│  │  7. Upload test results                    │  │
│  │  8. Upload code coverage                   │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │  deploy.yml - Deployment                   │  │
│  │  Triggers: Push to main (after CI)         │  │
│  │                                            │  │
│  │  1. Download build artifacts               │  │
│  │  2. Deploy Bicep infrastructure            │  │
│  │     (Azure resources via ADR-007)          │  │
│  │  3. Deploy to Azure App Service            │  │
│  │  4. Run database migrations                │  │
│  │  5. Smoke test deployed application        │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │  pr-validation.yml - PR Checks             │  │
│  │  Triggers: PR opened/updated               │  │
│  │                                            │  │
│  │  1. Validate PR naming convention          │  │
│  │  2. Run CI pipeline                        │  │
│  │  3. Check code coverage (≥90% per PRD)     │  │
│  │  4. Post coverage comment to PR            │  │
│  └────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

### Workflow Files Structure

```text
.github/
├── workflows/
│   ├── ci.yml                    # Continuous Integration
│   ├── deploy.yml                # Deployment to Azure
│   ├── pr-validation.yml         # PR checks and validation
│   └── codeql-analysis.yml      # Security scanning (future)
└── README.md                     # Documentation for workflows
```

## Consequences

### Positive

1. **GitHub Native** - No additional platforms, uses GitHub's built-in CI/CD (single pane of glass)
2. **YAML Configuration** - Pipeline-as-code stored in repository, versioned with application code
3. **Azure Integration** - Native GitHub Actions for Azure (azure/webapps-deploy, azure/login)
4. **Matrix Builds** - Can test across multiple OS/SDK versions if needed
5. **Marketplace** - 10,000+ pre-built actions available (reduce custom scripting)
6. **Cost Effective** - Free for public repos, generous minutes for private repos (ATLAS is private)
7. **PR Integration** - Status checks, coverage comments, and required checks block merges

### Negative

1. **GitHub Lock-in** - Pipelines are specific to GitHub (migrating to GitLab would require rewrite)
2. **Debugging Complexity** - Troubleshooting failed workflows requires reading logs in GitHub UI
3. **Secrets Management** - Azure credentials must be stored as GitHub Secrets (additional security consideration)
4. **Concurrent Job Limits** - Free tier has limits on parallel jobs (acceptable for ATLAS team size)
5. **YAML Learning Curve** - Team must learn GitHub Actions YAML syntax (mitigated by templates)

### Mitigations

- **Documentation** - Create `docs/engineering/ci-cd-guidelines.md` with examples
- **Templates** - Use workflow templates to standardize pipeline structure
- **Secrets Rotation** - Implement regular rotation of Azure service principal credentials
- **Local Testing** - Use `act` tool to test workflows locally before pushing
- **Fallback Plan** - Document manual deployment steps in case GitHub Actions is unavailable

## Alternatives Considered

### Azure DevOps Pipelines

- **ALT-001**: **Description**: Microsoft's dedicated CI/CD platform with advanced features
- **ALT-002**: **Rejection Reason**: Separate platform from GitHub, requires additional licenses for small team, YAML syntax similar to GitHub Actions but less integrated with PR workflow

### Jenkins

- **ALT-003**: **Description**: Self-hosted open-source automation server
- **ALT-004**: **Rejection Reason**: High maintenance overhead (server patching, plugin updates), not cloud-native, requires dedicated infrastructure

### GitLab CI

- **ALT-005**: **Description**: Integrated CI/CD in GitLab platform
- **ALT-006**: **Rejection Reason**: Would require migrating repository from GitHub to GitLab, losing GitHub's PR review features and ecosystem

## Implementation Notes

- **IMP-001**: Create `ci.yml` first with build and test steps (unit tests must pass before merge)
- **IMP-002**: Configure GitHub Environments for staging and production (requires approval before prod deployment)
- **IMP-003**: Store Azure credentials as GitHub Secrets (`AZURE_CREDENTIALS`, `AZURE_SUBSCRIPTION_ID`)
- **IMP-004**: Integrate code coverage enforcement (fail PR if coverage drops below 90% per PRD Quality Policy)
- **IMP-005**: Use Bicep deployment in `deploy.yml` (references ADR-007 Bicep templates)
- **IMP-006**: Add PR naming validation to enforce `feature/`, `fix/`, `docs/` prefixes (per PRD conventions)

## Compliance with Requirements

| Requirement | How GitHub Actions Addresses It |
| ----------- | ----------------------------- |
| PRD C-01: .NET 9 | ✅ Setup .NET 9 SDK in workflow |
| PRD Quality Policy: 90% coverage | ✅ Enforce in CI pipeline with coverage gates |
| PRD Branch/PR conventions | ✅ Validate PR titles match naming patterns |
| ADR-007: Bicep deployment | ✅ Deploy infrastructure as part of `deploy.yml` |
| ADR-003: Azure SQL + Blob | ✅ Run EF Core migrations and validate storage post-deploy |
| Security (NFR-06) | ✅ GitHub Secrets for Azure credentials, no credentials in code |

## References

- **REF-001**: [ADR-003: Azure SQL + Blob Storage](adr-003-azure-sql-blob.md)
- **REF-002**: [ADR-007: Bicep Infrastructure as Code](adr-007-bicep.md)
- **REF-003**: [ATLAS PRD - Quality Policy](../PRDs/atlas-mvp-prd.md#quality-policy)
- **REF-004**: [GitHub Actions Documentation](https://docs.github.com/en/actions)
- **REF-005**: [Azure GitHub Actions](https://github.com/Azure/actions)
- **REF-006**: [.NET 9 GitHub Actions Setup](https://learn.microsoft.com/en-us/dotnet/devops/github-actions)
