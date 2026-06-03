# ATLAS Design Documentation

This directory contains the architecture and design documentation for the ATLAS (Automated Tracking & Licensing Application System) platform.

## Architecture Overview

ATLAS is built using:

- **Clean Architecture** - Separation of concerns with Domain, Application, Infrastructure, and Presentation layers
- **Domain-Driven Design (DDD)** - Rich domain models with aggregates, entities, and value objects
- **CQRS (Command Query Responsibility Segregation)** - Separate models for reads and writes using MediatR
- **ASP.NET Core API** - RESTful backend services
- **Blazor Web App** - Interactive web frontend
- **EF Core** - ORM for Azure SQL Database
- **Azure Blob Storage** - Document storage

## Document Index

Read the documents in the following order for best understanding:

| # | Document | Description |
| --- | ----------- | ------------- |
| 0 | [README.md](README.md) | This file - Design documentation index |
| 1 | [01-context-diagram.md](01-context-diagram.md) | System Context Diagram - ATLAS and its external actors/systems |
| 2 | [02-container-diagram.md](02-container-diagram.md) | Container Diagram - Internal containers and their interactions |
| 3 | [03-domain-model.md](03-domain-model.md) | Domain Model - Core domain concepts and relationships |
| 4 | [04-core-entities.md](04-core-entities.md) | Core Entities - Detailed entity definitions with properties |
| 5 | [05-aggregate-roots.md](05-aggregate-roots.md) | Aggregate Roots - Aggregate boundaries and invariants |
| 6 | [06-bounded-contexts.md](06-bounded-contexts.md) | Bounded Contexts - Domain boundaries and context mapping |
| 7 | [07-data-flow.md](07-data-flow.md) | Data Flow Diagrams - Key process flows and interactions |
| 8 | [08-extension-points.md](08-extension-points.md) | Future Extension Points - Notifications, Service Bus, Reporting, Workflow |

## Architecture Decisions

Key architectural decisions are documented as ADRs (Architecture Decision Records) in the [docs/ADRs/](../ADRs/) directory:

- [adr-001-clean-architecture.md](../ADRs/adr-001-clean-architecture.md) - Adoption of Clean Architecture
- [adr-002-cqrs-mediatr.md](../ADRs/adr-002-cqrs-mediatr.md) - CQRS with MediatR pattern
- [adr-003-azure-sql-blob.md](../ADRs/adr-003-azure-sql-blob.md) - Data storage strategy

## Diagram Conventions

All diagrams use [Mermaid](https://mermaid.js.org/) syntax for version-controlled, renderable diagrams in VS Code and GitHub.

### Diagram Types Used

- **C4 Context/Container Diagrams** - System and container-level views
- **Class Diagrams** - Domain model and entity relationships
- **Sequence Diagrams** - Data flow and process interactions
- **Context Maps** - Bounded context relationships

## Related Documentation

- [Product Requirements Document](../PRDs/atlas-mvp-prd.md) - MVP requirements and user stories
- [Architecture Overview](../architecture/README.md) - High-level architecture references
- [Engineering Guidelines](../engineering/README.md) - Development standards

## Reading Guide

1. Start with the **Context Diagram** to understand ATLAS in its ecosystem
2. Review the **Container Diagram** to see internal system components
3. Study the **Domain Model** and **Core Entities** to understand the business domain
4. Understand **Aggregate Roots** and **Bounded Contexts** for DDD patterns
5. Follow the **Data Flow** diagrams for process understanding
6. Review **Extension Points** for future architecture evolution

<!-- © Capgemini 2025 -->
