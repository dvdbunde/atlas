# ADR 002: CQRS with MediatR

## Status

### Accepted

## Context

ATLAS has distinct read and write requirements:

### Write Operations (Commands)

- Submit application (complex validation, domain logic)
- Approve/reject application (state changes, business rules)
- Create/update permit types (admin operations)
- Upload documents (file handling, blob storage)

### Read Operations (Queries)

- List applications by status (dashboard views)
- Get application details (with documents, reviews)
- Search/filter applications (officer dashboard)
- Generate reports (future: analytics, audit logs)

**Challenges with traditional CRUD:**

1. **Mixed responsibilities** - Same model handles both reads and writes, leading to complex objects
2. **Performance trade-offs** - Optimizing for reads (denormalization) hurts writes (normalization)
3. **Blurred boundaries** - Hard to separate command and query logic in controllers
4. **Testing complexity** - Query tests need different setup than command tests

Alternative patterns considered:

- **Traditional CRUD** - Single model for reads/writes, simpler but less flexible
- **CQRS without MediatR** - Manual command/query separation, more boilerplate
- **Event Sourcing** - Store all events, rebuild state (overkill for MVP)

## Decision

We will implement **CQRS (Command Query Responsibility Segregation)** using **MediatR** library.

### Pattern Overview

```text
┌──────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│  Blazor Page → API Controller → MediatR → Handler        │
└──────────────────────────────────────────────────────────┘
                           │
          ┌────────────────┴────────────────┐
          │                                 │
    ┌─────▼─────┐                   ┌──────▼──────┐
    │  Commands │                   │   Queries   │
    │ (Write)   │                   │   (Read)    │
    └─────┬─────┘                   └──────┬──────┘
          │                                 │
    ┌─────▼─────┐                   ┌──────▼──────┐
    │ Domain    │                   │ Read Models │
    │ Logic     │                   │ (DTOs)      │
    └─────┬─────┘                   └──────┬──────┘
          │                                 │
    ┌─────▼─────┐                   ┌──────▼──────┐
    │ Database  │                   │ Database    │
    │ (Writes)  │                   │ (Reads)     │
    └───────────┘                   └─────────────┘
```

### MediatR Implementation

**Commands (Write):**

```csharp
// Command definition
public class SubmitApplicationCommand : IRequest<Guid>
{
    public Guid PermitTypeId { get; set; }
    public string CitizenNotes { get; set; }
    public List<DocumentUpload> Documents { get; set; }
}

// Command handler
public class SubmitApplicationCommandHandler : IRequestHandler<SubmitApplicationCommand, Guid>
{
    private readonly IApplicationRepository _repository;
    private readonly IMediator _mediator;

    public async Task<Guid> Handle(SubmitApplicationCommand request, CancellationToken ct)
    {
        var application = new Application(request.PermitTypeId, request.CitizenNotes);
        
        foreach (var doc in request.Documents)
            application.AddDocument(doc.FileName, doc.ContentType, doc.BlobUrl);

        application.Submit();
        await _repository.AddAsync(application, ct);
        
        // Publish domain event (handled by MediatR)
        await _mediator.Publish(new ApplicationSubmittedEvent(application.Id), ct);
        
        return application.Id;
    }
}
```

**Queries (Read):**

```csharp
// Query definition
public class GetApplicationsByStatusQuery : IRequest<List<ApplicationSummaryDto>>
{
    public ApplicationStatus Status { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}

// Query handler
public class GetApplicationsByStatusQueryHandler : IRequestHandler<GetApplicationsByStatusQuery, List<ApplicationSummaryDto>>
{
    private readonly IApplicationReadRepository _readRepository;

    public async Task<List<ApplicationSummaryDto>> Handle(GetApplicationsByStatusQuery request, CancellationToken ct)
    {
        return await _readRepository.GetByStatusAsync(request.Status, request.Skip, request.Take, ct);
    }
}
```

### API Controller Usage

```csharp
[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Submit(SubmitApplicationCommand command)
    {
        var applicationId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = applicationId }, applicationId);
    }

    [HttpGet]
    public async Task<IActionResult> GetByStatus([FromQuery] ApplicationStatus status)
    {
        var result = await _mediator.Send(new GetApplicationsByStatusQuery { Status = status });
        return Ok(result);
    }
}
```

## Consequences

### Positive

1. **Separation of Concerns** - Commands and queries have distinct models, handlers, and validation
2. **Scalability** - Read and write sides can scale independently (future: separate databases)
3. **Testability** - Command handlers and query handlers tested in isolation
4. **MediatR Benefits**:
   - **Pipeline Behaviors** - Add cross-cutting concerns (logging, validation, auth) via pipelines
   - **Domain Events** - Publish events from handlers, decoupled event handling
   - **Reduced Coupling** - Controllers depend on MediatR, not concrete handlers
5. **Performance Optimization** - Read models can be denormalized for specific queries
6. **Future-Proof** - Easy to add read model projections (see [Extension Points](..\design\08-extension-points.md#3-reporting--analytics))

### Negative

1. **Complexity** - More classes (command/query objects, handlers, DTOs)
2. **Learning Curve** - Team must understand MediatR and CQRS concepts
3. **Indirect Flow** - Harder to trace code flow (controller → MediatR → handler)
4. **Small Operations** - Simple CRUD becomes multiple files

### Mitigations

- **Use MediatR Pipelines** - Centralize validation (FluentValidation), logging, performance monitoring
- **Code Generation** - Use templates or scaffolding for boilerplate
- **Clear Naming** - `{Action}{Entity}Command` and `{Action}{Entity}Query` conventions
- **Documentation** - Document CQRS patterns in `docs/architecture/`

### Pipeline Behavior Example

```csharp
// Validation pipeline (auto-validates commands/queries with FluentValidation)
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

## Compliance with Requirements

| Requirement | How CQRS + MediatR Addresses It |
| ------------- | -------------------------------------- |
| Domain logic (DDD) | Commands invoke domain methods (e.g., `application.Submit()`) |
| Audit logging | Domain events published via MediatR → audit log handler |
| Notifications | Domain events → notification handler (future extension) |
| Testability | Handlers unit-testable with mock repositories |
| Performance (500 concurrent users) | Read models optimized for queries, caching possible |

## References

- [ADR-001: Clean Architecture](adr-001-clean-architecture.md)
- [CQRS Pattern - Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [MediatR Library](https://github.com/jbogard/MediatR)
- [FluentValidation](https://fluentvalidation.net/)
- [ADR-003: Data Storage Strategy](adr-003-azure-sql-blob.md)
- [ADR-004: Domain-Driven Design](adr-004-domain-driven-design.md) (Domain Events)
- [ADR-005: Blazor Web App](adr-005-blazor-web-app.md) (MediatR in Blazor components)
