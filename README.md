# ATLAS - Case Management & Permit Processing Platform

**ATLAS** (Automated Tracking & Licensing Application System) is a modern case management and permit processing platform designed for local government. It digitizes the end-to-end permit application workflow, providing transparency for citizens, efficiency for permit officers, and control for administrators.

## What is ATLAS?

ATLAS replaces paper-based and disconnected permit processing with a unified digital platform:

- **Citizens** can submit permit applications, upload supporting documents, and track application status online
- **Permit Officers** can review applications, add notes, and approve or reject permits through a streamlined dashboard
- **Administrators** can manage permit types and view complete audit history for compliance

## Technology Stack

ATLAS is built with modern Microsoft technologies, targeting Azure for production deployment:

- **.NET 9** with **ASP.NET Core** (backend framework)
- **Blazor Server** (interactive web UI)
- **SQL Server** (Azure SQL Database target; LocalDB for local development)
- **Azure Blob Storage** (document storage and email-template persistence; Azurite emulator for local development)
- **Azure Communication Services (ACS) Email** (email delivery via Managed Identity)
- **Microsoft Entra ID** (authentication & authorization)
- **Azure App Service** (hosting platform; API and Blazor App Services)
- **Azure Key Vault** (secrets management, e.g. SQL connection string)

## Project Status

**Current Milestone**: M11 — Observability & Operations (Workbook validation remaining)

**Next Milestone**: Complete M11 Workbook validation (see [ROADMAP.md](plans/ROADMAP.md))

**Completed Features**:

- ✅ **M1: Solution Foundation** — Clean Architecture with 4 layers (Domain, Application, Infrastructure, API), GitHub Actions CI pipeline
- ✅ **M2: Domain Model** — DDD entities, aggregates, value objects, domain events with rich business logic
- ✅ **M3: Database Persistence** — EF Core with LocalDB, repository pattern, UnitOfWork, data migrations
- ✅ **M4: Authentication** — Microsoft Entra ID with role-based authorization (Citizen, Officer, Admin)
- ✅ **M5: Permit Submission** — Draft applications, dynamic forms, citizen dashboard, application detail/edit, confirmation workflow, email notifications
- ✅ **M6: Document Management** — Azure Blob Storage integration, upload/download, SAS token security, FileUpload field type
- ✅ **M7: Officer Review Workflow** — Officer dashboard, application review, approve/reject/request-info workflow, internal notes, activity timeline
- ✅ **M8: Administration Portal** — Admin dashboard, permit type designer, User Directory (read-only), Audit Log viewer, Email Template administration, Application Explorer
- ✅ **M9: Azure Infrastructure & Deployment** — Production-ready Azure infrastructure using Bicep (App Services, Azure SQL, Key Vault, Blob Storage, Container Registry, Application Insights, Log Analytics, Azure Communication Services); GitHub Actions CI/CD with workload identity federation; fully automated, idempotent deployment and bootstrap using Managed Identity and RBAC
- ✅ **M10: Azure Communication Services Email Integration** — Azure Communication Services email delivery with Managed Identity; email templates persisted in Azure Blob Storage (customized templates override source-code defaults, with Reset-to-default); local development uses a deterministic local email sink
- 🚧 **M11: Observability & Operations** — Application Insights + Log Analytics, distributed tracing and custom metrics, Azure Monitor diagnostics and alerts, Grafana Cloud operational dashboard, Operations Portal observability, and Operations Workbook deployment; final Workbook functional validation remains

## Who this is for

- **Developers** contributing to the ATLAS platform
- **Product Owners** and stakeholders tracking requirements and progress
- **Local government staff** (permit officers, administrators) providing domain expertise
- **AI agents** (including GitHub Copilot) needing repository context for development tasks

## Repository Structure

This repository contains both the ATLAS application code and comprehensive documentation:

### Application Code

- `src/` - Application source code (.NET 9, Blazor)

### Documentation & Planning

- `docs/` - Product requirements, architecture, design documents, and engineering guidelines
  - [PRDs](docs/PRDs/) - Product Requirements Documents (e.g., [ATLAS MVP PRD](docs/PRDs/atlas-mvp-prd.md))
  - [ADRs](docs/ADRs/) - Architectural Decision Records
  - [Design Docs](docs/design/) - Technical design specifications
  - [Engineering Guidelines](docs/engineering/) - Development process documentation
    - [Contract Governance](docs/engineering/contract-governance.md) - Contract-first development workflow
  - [Runbooks](docs/runbooks/) - Operational runbooks (e.g., [Email Delivery Deployment Checklist](docs/architecture/email-delivery-deployment-checklist.md))

### Project Management

- `plans/` - Project plans, roadmap, and task tracking
  - [ROADMAP.md](plans/ROADMAP.md) - Strategic planning and milestones

### Development Configuration

- `.github/` - GitHub configuration including Copilot customizations
  - [Agents](.github/agents/) - Custom AI agents for development tasks
  - [Instructions](.github/instructions/) - Coding standards and guidelines
  - [Prompts](.github/prompts/) - Reusable prompt templates for documentation

## How to get started

1. **Understand the project**: Read the [ATLAS MVP PRD](docs/PRDs/atlas-mvp-prd.md) for requirements and scope
2. **Set up development environment**: Ensure you have .NET 9 SDK installed (run `dotnet --list-sdks` to verify)
3. **Configure local database**: ATLAS uses LocalDB for development. Run `dotnet ef database update` from the Infrastructure project to create the schema
4. **Configure authentication**: See [Authentication Setup](#authentication-setup) below for Microsoft Entra ID configuration
5. **Azure Storage emulation**: Document upload/download uses Azure Blob Storage. For local development, Azurite storage emulator is used automatically (configured in `appsettings.Development.json`). Install Azurite via `npm install -g azurite` and start it with `azurite`
6. **Email behavior**: In local development, email delivery uses a deterministic local sink (no external service required). In Azure, email is delivered via Azure Communication Services using Managed Identity. Email templates are persisted in Blob Storage and can be administered from the Administration Portal
7. **Review architecture**: See [architecture documentation](docs/architecture/)
8. **Review coding standards**: Read [backend instructions](.github/instructions/backend.instructions.md) and [frontend instructions](.github/instructions/frontend.instructions.md)
9. **Check the roadmap**: Review [ROADMAP.md](plans/ROADMAP.md) for current priorities

## Authentication Setup

ATLAS uses Microsoft Entra ID for authentication. Local development requires:

1. An Entra ID tenant with app registrations for the Blazor UI and API
2. App roles configured: `Citizen`, `Officer`, `Admin`
3. Configuration in `src/ATLAS.API/appsettings.json` under the `AzureAd` section:
   - `TenantId`, `ClientId`, `Audience` for the API
   - `SwaggerClientId` for Swagger UI OAuth2 flow

See ADR-008 and ADR-013 for architecture details.

## Running Locally

```bash
# Start the API
dotnet run --project src/ATLAS.API/

# In a separate terminal, start the Blazor frontend (requires API running)
dotnet run --project src/ATLAS.Blazor/
```

The Blazor Server app should be started in a terminal alongside the API.

## Building

```bash
dotnet build ATLAS.slnx
```

## Testing

```bash
dotnet test ATLAS.slnx
```

## Development Workflow

### Contract-First Development

ATLAS follows **contract-first development** for API changes. The OpenAPI specification (`openapi/atlas-api.yaml`) is the **single source of truth**.

**Key principles:**

- All API changes MUST start with `openapi/atlas-api.yaml`
- Generated files (`GeneratedControllers.g.cs`, `AtlasContracts.g.cs`) are NEVER edited manually
- Run `dotnet build` to regenerate artifacts before committing
- Use `scripts/validate-contract.ps1` to validate contract locally

**Documentation:**

- [Contract Governance](docs/engineering/contract-governance.md) - Complete guide
- [PR Template](.github/PULL_REQUEST_TEMPLATE.md) - Contract checklist
- [CI Workflow](.github/workflows/contract-validation.yml) - Automated validation

### Branching Strategy

The repository enforces trunk-based development with:

- Short-lived feature branches (maximum 3 days)
- Mandatory pull request reviews
- Squash and merge for clean history
- Automatic branch cleanup

### Commit Conventions

All commits must follow conventional commit format:

```text
<type>: <subject>

[optional body]

[optional footer]
```

Types include: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

### Branch Naming

- `feature/` - New features or enhancements
- `fix/` - Bug fixes and hotfixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring without functional changes
- `test/` - Test additions or modifications
- `plan/` - Planning artifacts and proposals

## Documentation Resources

### Copilot Customization

This repository includes comprehensive GitHub Copilot configuration:

- **Agents**: Specialized AI assistants for development tasks (Developer, Code Reviewer, Tester)
- **Instructions**: Coding standards and guidelines for backend, frontend, documentation, and testing
- **Prompts**: Reusable templates for creating ADRs, PRDs, documentation, and specifications

### Key Reference Patterns

#### Central Configuration Hub

- `.github/copilot-instructions.md` serves as the primary configuration document, referencing most instruction files and core documentation

#### Documentation Workflow

- Prompt files like `write-adr.prompt.md` and `write-prd.prompt.md` reference their respective templates and directories
- `docs.instructions.md` defines standards for all documentation types and their storage locations

#### Planning Integration

- Plans reference core configuration files and maintain the TODO workflow
- The built-in Planner agent (VS Code) integrates with the plans structure and references the plan template

## Where to find more information

- See `.github/copilot-instructions.md` for Copilot-specific rules and configuration
- See [ATLAS MVP PRD](docs/PRDs/atlas-mvp-prd.md) for complete product requirements

### Copilot Customisation

You can find more about how to [customise and extend GitHub Copilot](https://docs.github.com/en/copilot/how-to/customize-copilot/add-repository-instructions?tool=vscode), or how to customise Copilot behaviour in [Visual Studio Code](https://code.visualstudio.com/docs/copilot/customization/overview), and other IDEs such as [Jetbrains](https://docs.github.com/en/copilot/how-to/customize-copilot/add-repository-instructions?tool=jetbrains), [Eclipse](https://docs.github.com/en/copilot/how-to/customize-copilot/add-repository-instructions?tool=eclipse), and [XCode](https://docs.github.com/en/copilot/how-to/customize-copilot/add-repository-instructions?tool=xcode).

You can find more examples of Copilot configuration in the [Awesome Copilot repository on GitHub.com](https://github.com/github/awesome-copilot/tree/main).

## Appendix: SSOT Source Map

Authoritative single sources of truth (SSOT) for key policies and templates. Prefer linking to these instead of duplicating content.

- **Core policies and workflow**
  - Copilot instructions (SSOT): `.github/copilot-instructions.md`
    - Quality & Coverage Policy: `.github/copilot-instructions.md#quality-policy`

- **Contract Governance**
  - Contract Governance (SSOT): `docs/engineering/contract-governance.md`
    - Contract-first development workflow: `docs/engineering/contract-governance.md#principles`
    - Versioning strategy: `docs/engineering/contract-governance.md#versioning-strategy`
    - Forbidden practices: `docs/engineering/contract-governance.md#forbidden-practices`
  - OpenAPI Specification (SSOT): `openapi/atlas-api.yaml`
  - Validation script: `scripts/validate-contract.ps1`
  - CI Workflow: `.github/workflows/contract-validation.yml`

- **Engineering guidelines**
  - Code review checklist (SSOT): `docs/engineering/code-review-guidelines.md#code-review-checklist`
  - Pull request guidelines: `docs/engineering/pull-request-guidelines.md`

- **Documentation**
  - Docs authoring rules (SSOT): `.github/instructions/docs.instructions.md`
  - Documentation flow anchor: `.github/instructions/docs.instructions.md#documentation-process-flow`

- **Testing**
  - BDD feature guidance (SSOT): `.github/instructions/bdd-tests.instructions.md`
  - Tester agent (enforces policy): `.github/agents/Tester.agent.md`

- **Backend**
  - Backend instructions (SSOT): `.github/instructions/backend.instructions.md`
  - Architecture: `.github/instructions/backend.instructions.md#backend-architecture`
  - Error handling: `.github/instructions/backend.instructions.md#backend-error-handling`
  - Observability: `.github/instructions/backend.instructions.md#backend-observability`
  - Security: `.github/instructions/backend.instructions.md#backend-security`

- **Planning**
  - Plan template (SSOT): `plans/plan-template.md`
  - Small plan example: `plans/examples/plan-small.md`

### CI Coverage Enforcement

This repo includes a minimal coverage enforcement workflow (`.github/workflows/coverage.yml`) and script (`scripts/enforce-coverage.js`) aligned with the Quality & Coverage Policy:

- Global ≥ 90%; core modules ≥ 95%; integrations ≥ 85%; critical/hot/error/security paths 100%.
- The sample job looks for a Jest `coverage/coverage-summary.json`. Adapt the test step for your stack (e.g., Python `coverage.json`, Java `jacoco.xml` converted to JSON) and point the script to the generated summary.
- Branching & Workflow: see "Project Methodologies" in `.github/copilot-instructions.md`
- Naming & Commit Conventions: see corresponding sections in the same file.

Notes:

- Chat modes and prompts should reference these SSOT files. Avoid duplicating numeric thresholds, templates, or process steps in multiple places.
- CI tasks (if added) should validate adherence to SSOT anchors where practical.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/)

### Build

`ash
dotnet build
`

### Run Tests

`ash
dotnet test
`

### Run API

`ash
cd src/ATLAS.Api
dotnet run
`

### Run Blazor UI

`ash
cd src/ATLAS.Blazor
dotnet run
`

## Project Structure

ATLAS follows Clean Architecture principles with 4 layers:

- **Domain** (src/ATLAS.Domain) - Enterprise business rules and entities
- **Application** (src/ATLAS.Application) - Application business rules, CQRS with MediatR
- **Infrastructure** (src/ATLAS.Infrastructure) - External dependencies and data access
- **Presentation** (src/ATLAS.Api, src/ATLAS.Blazor) - API and UI

## Development Standards

- **TDD**: Write failing tests first, then implement minimal code
- **Clean Architecture**: Dependencies flow inward toward the Domain
- **CQRS**: Commands and queries separated using MediatR
- **Validation**: FluentValidation for business rules

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for detailed coding standards.
