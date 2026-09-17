# ATLAS — Case Management & Permit Processing Platform

**ATLAS** (**A**utomated **T**racking & **L**icensing **A**pplication **S**ystem) is a personal, full-stack software project that demonstrates a digital platform for managing permit applications and related casework.

The platform is designed around a local-government scenario, with separate experiences for citizens, permit officers, and administrators.

## Overview

ATLAS supports the main stages of a permit-processing lifecycle:

- Citizens can create and save draft applications.
- Citizens can submit applications, provide supporting documents, view application details, and respond to requests for additional information.
- Permit officers can view, assign, release, and review applications.
- Officers can request additional information, approve applications, or reject applications.
- Administrators can manage permit types, configure dynamic forms, manage users and email templates, explore applications, and inspect audit information.
- The platform records application activity and workflow decisions to support traceability and accountability.

The project is intended as a realistic technical demonstration and learning project rather than a production service for a specific government organization.

## Main capabilities

### Citizen

- Citizen dashboard
- Dynamic permit application forms
- Draft creation and editing
- Application submission
- Supporting-document upload and management
- Application status and detail views
- Resubmission after an information request
- Visibility into relevant application activity and review information

### Permit officer

- Officer dashboard
- Application review
- Application assignment and self-release
- Submitted application data and document review
- Request for additional information
- Application approval and rejection
- Review history and activity timeline

### Administrator

- Administrative dashboard
- Application Explorer
- Permit type management and dynamic form designer
- Reference-data management
- User directory and user details
- Audit log viewer
- Email template administration
- Operational and observability information

## Technology stack

- **.NET 9**
- **ASP.NET Core**
- **Blazor Server**
- **C#**
- **Entity Framework Core**
- **SQL Server / Azure SQL Database**
- **Azure Blob Storage** for documents and persisted email templates
- **Azurite** for local Blob Storage emulation
- **Azure Communication Services Email**
- **Microsoft Entra ID** for authentication and authorization
- **Azure App Service**
- **Azure Key Vault**
- **Azure Container Registry**
- **Application Insights and Log Analytics**
- **Docker**
- **GitHub Actions**
- **Bicep** for Azure infrastructure as code
- **MediatR / CQRS**
- **Clean Architecture and domain-driven design principles**

## Architecture

ATLAS follows a layered Clean Architecture structure:

```text
src/
├── ATLAS.Domain          Domain entities, enums, business rules, domain events
├── ATLAS.Application     Use cases, commands, queries, DTOs, validation
├── ATLAS.Infrastructure  EF Core, persistence, Azure integrations, external services
├── ATLAS.API             HTTP API and API infrastructure
└── ATLAS.Blazor          Blazor Server user interface
```

The application layer uses CQRS-style commands and queries, with MediatR coordinating application use cases. Dependencies are intended to flow inward toward the Domain layer.

## Project status

The original implementation milestones established the platform foundation, domain model, persistence, authentication, citizen submission, document management, officer review, administration, Azure infrastructure, email integration, and observability.

The most recently completed milestone is:

>**M12 — UI Restyling and Usability Improvements**

M12 included, among other improvements:

- Consistent visual styling across Citizen, Officer, and Admin areas
- Shared layout and presentation improvements
- Application-detail and review-page refinements
- Improved workflow-action presentation
- Date/time display standardization
- Removal of redundant or misleading UI elements
- Officer assignment and decision-action refinements
- Citizen edit and resubmission layout improvements

The next milestone has not yet been selected. Candidate topics are being evaluated with a preference for additive functionality that complements the existing workflows without redesigning or disrupting them.

For milestone plans and task tracking, see the `plans/` directory.

## Local development

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git
- SQL Server LocalDB or another configured SQL Server instance
- Azurite, when using local Blob Storage emulation
- Access to a configured Microsoft Entra ID tenant for authenticated scenarios

### Configuration

Local configuration is held in the relevant `appsettings` files and local development configuration.

Do not commit secrets, credentials, connection strings containing secrets, or tenant-specific sensitive configuration to source control.

The application may require configuration for:

- Database access
- Microsoft Entra ID
- Blob Storage
- Azure Communication Services, where applicable
- Other environment-specific integrations

### Start the API

```bash
dotnet run --project src/ATLAS.API/
```

### Start the Blazor application

In a separate terminal:

```bash
dotnet run --project src/ATLAS.Blazor/
```

The Blazor application should be run alongside the API when the selected configuration requires the API.

### Local storage and email

For local development:

- SQL Server LocalDB is used where configured.
- Azurite can emulate Azure Blob Storage.
- Email uses the configured local development behavior and does not require production email delivery.
- Azure deployments use Azure services and managed identity according to the deployment configuration.

## Build and test

Build the solution:

```bash
dotnet build ATLAS.slnx
```

Run the test suite:

```bash
dotnet test ATLAS.slnx
```

Run contract validation where applicable:

```powershell
scripts/validate-contract.ps1
```

## API contract governance

ATLAS follows a contract-first approach for API changes.

The OpenAPI specification is maintained at:

```text
openapi/atlas-api.yaml
```

Key rules include:

- API changes begin with the OpenAPI contract.
- Generated API artifacts must not be edited manually.
- Contract validation should be run before changes are committed.
- API and implementation changes should remain consistent with the documented contract.

See:

- [Contract Governance](docs/engineering/contract-governance.md)
- [OpenAPI specification](openapi/atlas-api.yaml)
- [CI workflow](.github/workflows/ci.yml)
- [Package workflow](.github/workflows/01-package.yml)
- [Development deployment workflow](.github/workflows/02-deploy-dev.yml)

## Development practices

The repository uses:

- Clean Architecture
- Domain-driven design principles
- CQRS with MediatR
- Test-first development where practical
- Automated unit, integration, and component testing
- Contract-first API development
- Structured logging and observability
- Auditability for state-changing operations
- Trunk-based development
- Short-lived branches and pull requests
- Conventional commits

The `.github/` directory contains repository-specific instructions, agents, prompts, and development conventions for human and AI-assisted development.

## Repository structure

```text
.
├── src/                    Application source code
├── docs/                   Product, architecture, ADR, design, and engineering documentation
├── plans/                  Milestone plans and task tracking
├── openapi/                API contract
├── scripts/                Development and validation scripts
├── .github/                CI/CD, agents, instructions, and prompts
└── ATLAS.slnx              Solution file
```

Useful documentation locations:

- `docs/PRDs/` — Product requirements
- `docs/ADRs/` — Architectural Decision Records
- `docs/architecture/` — Architecture and deployment documentation
- `docs/engineering/` — Engineering guidelines
- `docs/runbooks/` — Operational runbooks
- `plans/` — Roadmap, milestone plans, and tasks
- `.github/copilot-instructions.md` — Main AI-assisted development guidance

## Documentation status

This README describes the current implemented baseline. Historical milestone plans, architectural decisions, and original product requirements are retained in `docs/`, `plans/`, and `docs/ADRs/`; those documents may describe the state or intent at the time they were written.

## Deployment

ATLAS includes Azure infrastructure and deployment automation using:

- Azure App Services
- Azure SQL Database
- Azure Blob Storage
- Azure Key Vault
- Azure Container Registry
- Application Insights
- Log Analytics
- Azure Communication Services
- Managed Identity and role-based access control
- Bicep
- GitHub Actions with workload identity federation

Deployment details, prerequisites, configuration, and operational procedures are documented under `docs/architecture/` and `docs/runbooks/`.

## Project purpose

ATLAS is primarily a personal software engineering and learning project. It is used to:

- Maintain and update full-stack .NET development skills
- Explore current Microsoft and Azure technologies
- Practise architecture, domain modelling, testing, DevOps, and cloud deployment
- Demonstrate the design and implementation of a complete case-management application

## Additional information

For product scope, begin with the relevant PRD in `docs/PRDs/`.

For development rules and AI-agent guidance, begin with:

```text
.github/copilot-instructions.md
```

For current milestone work, consult the plans and task files under:

```text
plans/
```
