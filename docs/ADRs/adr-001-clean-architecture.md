# ADR 001: Adopt Clean Architecture

## Status

**Accepted**

## Context

ATLAS is a new greenfield project for a local government permit processing platform. We need an architecture that:

1. **Separates concerns** - Business logic must be independent of frameworks, UI, and databases
2. **Supports testing** - Domain logic should be unit-testable without infrastructure dependencies
3. **Enables evolution** - Framework choices (Blazor, EF Core, Azure) should be replaceable
4. **Aligns with DDD** - Supports Domain-Driven Design patterns (entities, aggregates, value objects)
5. **Scales with team** - Clear boundaries allow multiple developers to work in parallel

Alternative architectures considered:

- **Layered Architecture** (Presentation → Business → Data Access) - Simpler but couples business logic to frameworks
- **Hexagonal Architecture (Ports & Adapters)** - Similar benefits but less familiar to .NET teams
- **Monolithic MVC** - Too coupled, hard to test, framework-dependent

## Decision

We will adopt **Clean Architecture** as described by Robert C. Martin (Uncle Bob).

### Architecture Layers (inside-out)

```
┌─────────────────────────────────────────────────┐
│           Presentation Layer                    │
│    (Blazor Web App, Controllers, ViewModels)    │
├─────────────────────────────────────────────────┤
│           Application Layer                     │
│    (CQRS Commands/Queries, DTOs, Interfaces)    │
├─────────────────────────────────────────────────┤
│           Domain Layer                          │
│    (Entities, Aggregates, Value Objects,        │
│     Domain Events, Domain Services)             │
├─────────────────────────────────────────────────┤
│           Infrastructure Layer                  │
│    (EF Core, Azure SDKs, Repositories,          │
│     External Service Integrations)              │
└─────────────────────────────────────────────────┘
```

### Dependency Rule

**Dependencies only point inward.** Outer layers depend on inner layers, never the reverse.

- Presentation → Application → Domain ← Infrastructure
- Domain layer has **no dependencies** on external frameworks or libraries
- Infrastructure implements interfaces defined in Application or Domain layers

### Project Structure

```
src/
├── Atlas.Domain/              # Domain Layer (no external deps)
│   ├── Entities/
│   ├── Aggregates/
│   ├── ValueObjects/
│   ├── Events/
│   └── Interfaces/           # Repository interfaces, service interfaces
│
├── Atlas.Application/         # Application Layer
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   ├── Validators/
│   └── Interfaces/           # Service interfaces
│
├── Atlas.Infrastructure/     # Infrastructure Layer
│   ├── Data/                # EF Core DbContext, migrations
│   ├── Repositories/        # Repository implementations
│   ├── Services/            # External service integrations
│   └── BlobStorage/
│
├── Atlas.API/                # Presentation Layer (API)
│   ├── Controllers/
│   ├── Models/
│   └── Program.cs
│
└── Atlas.Blazor/             # Presentation Layer (Web UI)
    ├── Pages/
    ├── Components/
    └── Program.cs
```

## Consequences

### Positive

1. **Testability** - Domain logic can be unit tested without databases, web frameworks, or external services
2. **Framework Independence** - Business rules are not coupled to Blazor, EF Core, or Azure SDKs
3. **Flexibility** - Can replace EF Core with Dapper, or Blazor with React, without touching business logic
4. **Clear Boundaries** - Developers know exactly where code belongs (Domain vs Application vs Infrastructure)
5. **DDD Alignment** - Naturally supports entities, aggregates, value objects, and domain events
6. **CQRS Integration** - Clean separation makes CQRS implementation straightforward

### Negative

1. **Complexity** - More projects/assemblies than a simple layered architecture
2. **Learning Curve** - Team must understand Dependency Inversion principle and layer responsibilities
3. **Boilerplate** - More interfaces and mapping code (mitigated by MediatR and AutoMapper)
4. **Small Feature Overhead** - Simple CRUD operations require changes across multiple layers

### Mitigations

- **Use MediatR** - Reduces boilerplate in Application layer (CQRS handlers)
- **Use AutoMapper** - Reduces mapping code between entities and DTOs
- **Provide Templates** - Create project templates and code snippets for common patterns
- **Training** - Conduct team workshop on Clean Architecture and DDD principles

## Compliance with Requirements

| Requirement | How Clean Architecture Addresses It |
|-------------|-------------------------------------|
| 99.9% uptime | Infrastructure layer can implement caching, retries, circuit breakers |
| Security (Entra ID, RBAC) | Authentication/authorization in Presentation layer, user context passed to Domain |
| Azure SQL + Blob Storage | Data access abstracted in Infrastructure, Domain unaware of storage technology |
| Testability | Domain layer unit-testable; Application layer testable with mock repositories |
| Future extensions (notifications, reporting) | New concerns added as new projects, following dependency rule |

## References

- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [ATLAS PRD - Technology Stack](../PRDs/atlas-mvp-prd.md#technology-stack)
- [Next ADR: CQRS with MediatR](adr-002-cqrs-mediatr.md)
