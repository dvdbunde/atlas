---
applyTo: "**/*.cs, **/*.razor, **/*.csproj"
---

<!--
==============================================================================
ATLAS-SPECIFIC INSTRUCTIONS
==============================================================================
PURPOSE: Provide Copilot guidance specific to the ATLAS case management system
AUDIENCE: AI assistants working on ATLAS codebase
SCOPE: Architecture rules, domain patterns, and public-sector compliance requirements
==============================================================================
-->

# ATLAS Copilot Instructions

<!--
SECTION PURPOSE: Introduce ATLAS system context and architectural constraints
PROMPTING TECHNIQUES: Domain context priming, architectural guardrails
-->

## System Context

ATLAS is a **public-sector case management system** built with:
- **Clean Architecture** - Strict layer separation
- **Domain-Driven Design (DDD)** - Rich domain model with aggregates
- **CQRS** - Command/Query separation via MediatR
- **EF Core** - Persistence with SQL Server
- **Blazor** - Interactive web UI

<!--
SECTION PURPOSE: Define mandatory architectural constraints
PROMPTING TECHNIQUES: XML semantic tags for critical requirements, visual enforcement
-->

## Architectural Rules

<CRITICAL_REQUIREMENT type="MANDATORY">
AI assistants MUST enforce these architectural constraints when generating or modifying ATLAS code.
</CRITICAL_REQUIREMENT>

### Layer Dependencies

**Domain Layer Isolation:**
- ✅ Domain layer MUST NOT reference Infrastructure or Presentation layers
- ✅ Domain layer MAY reference only .NET base libraries and shared kernel
- ❌ No EF Core, MediatR, or Blazor dependencies in Domain project
- ❌ No database, HTTP, or UI concerns in domain entities

**Dependency Flow:**
```
Presentation (Blazor) → Application (CQRS/MediatR) → Domain ← Infrastructure (EF Core)
```

### Domain Modeling Rules

<CRITICAL_REQUIREMENT type="MANDATORY">
Business rules MUST be encapsulated in domain aggregates, not scattered across services.
</CRITICAL_REQUIREMENT>

**Aggregate Design:**
- Business rules belong in aggregate roots and entities
- Use domain events for state changes (`IDomainEvent`)
- Maintain aggregate invariants through private setters and factory methods
- Validate in constructor/factory, not setters

**Value Objects Over Primitives:**
- ✅ Prefer `CaseNumber` over `string caseNumber`
- ✅ Prefer `Email` over `string email`
- ✅ Prefer `Money` over `decimal amount`
- Use `ValueObject` base class for equality semantics

**Example Pattern:**
```csharp
// ✅ GOOD: Value object with validation
public class CaseNumber : ValueObject
{
    public string Value { get; }
    
    private CaseNumber(string value) => Value = value;
    
    public static Result<CaseNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<CaseNumber>("Case number cannot be empty");
        
        if (!Regex.IsMatch(value, @"^CASE-\d{6}$"))
            return Result.Failure<CaseNumber>("Invalid case number format");
            
        return new CaseNumber(value);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

// ❌ BAD: Primitive obsession
public class Case
{
    public string CaseNumber { get; set; } // No validation, no semantics
}
```

### CQRS with MediatR

**Command Pattern:**
- Commands represent state changes (Create, Update, Delete)
- Command handlers live in Application layer
- Return `Result<T>` or `Unit` from handlers
- Validate with FluentValidation or MediatR pipeline behaviors

**Query Pattern:**
- Queries return read-only DTOs
- Use separate read models (not domain entities)
- Optimize for UI needs (flattened, projected)

**Example:**
```csharp
// Command
public record CreateCaseCommand(
    string CaseNumber,
    string ClientName,
    string CaseType
) : IRequest<Result<Guid>>;

public class CreateCaseCommandHandler 
    : IRequestHandler<CreateCaseCommand, Result<Guid>>
{
    private readonly ICaseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<Result<Guid>> Handle(
        CreateCaseCommand request, 
        CancellationToken cancellationToken)
    {
        var caseNumberResult = CaseNumber.Create(request.CaseNumber);
        if (caseNumberResult.IsFailure)
            return Result.Failure<Guid>(caseNumberResult.Error);
            
        var @case = Case.Create(caseNumberResult.Value, request.ClientName);
        
        await _repository.AddAsync(@case, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return @case.Id;
    }
}
```

### Auditability Requirements

<CRITICAL_REQUIREMENT type="MANDATORY">
Every state change in ATLAS MUST be auditable. AI assistants MUST include audit mechanisms in generated code.
</CRITICAL_REQUIREMENT>

**Audit Patterns:**
- Raise domain events for all state changes (`CaseCreated`, `CaseStatusChanged`)
- Store audit trail in separate audit table or append-only log
- Include: who, what, when, old value, new value
- Use EF Core's `ChangeTracker` for automatic auditing

**Example:**
```csharp
public class Case : AggregateRoot
{
    public CaseNumber CaseNumber { get; private set; }
    public CaseStatus Status { get; private set; }
    
    private Case() { } // EF Core
    
    public static Case Create(CaseNumber caseNumber, string clientName)
    {
        var @case = new Case
        {
            Id = Guid.NewGuid(),
            CaseNumber = caseNumber,
            ClientName = clientName,
            Status = CaseStatus.New
        };
        
        @case.RaiseDomainEvent(new CaseCreatedEvent(@case.Id, caseNumber.Value));
        return @case;
    }
    
    public void ChangeStatus(CaseStatus newStatus, string reason)
    {
        if (Status == newStatus)
            return;
            
        var oldStatus = Status;
        Status = newStatus;
        
        RaiseDomainEvent(new CaseStatusChangedEvent(
            Id, oldStatus, newStatus, reason, DateTime.UtcNow));
    }
}
```

<!--
SECTION PURPOSE: Enforce public-sector compliance priorities
PROMPTING TECHNIQUES: Priority hierarchy, concrete examples
-->

## Public-Sector Priorities

<CRITICAL_REQUIREMENT type="MANDATORY">
ATLAS code MUST prioritize these in order: Auditability > Security > Accessibility > Maintainability
</CRITICAL_REQUIREMENT>

### 1. Auditability (Highest Priority)

**Requirements:**
- All data modifications logged with user ID and timestamp
- Audit logs immutable and append-only
- Support forensic analysis and compliance reporting
- Track both data changes and access events

**Code Patterns:**
```csharp
// Include audit fields in all entities
public abstract class AuditableEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public string CreatedBy { get; protected set; }
    public DateTime? ModifiedAt { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public byte[] RowVersion { get; protected set; } // Concurrency
}

// Audit log entry
public class AuditLog
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string Action { get; private set; }
    public string EntityName { get; private set; }
    public Guid EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string IpAddress { get; private set; }
}
```

### 2. Security

**Requirements:**
- Authentication via Entra ID (Azure AD)
- Role-based access control (RBAC) for case access
- Data encryption at rest and in transit
- Input validation on all public methods
- SQL injection prevention (parameterized queries via EF Core)

**Code Patterns:**
```csharp
// Authorization attribute on Blazor pages
@attribute [Authorize(Roles = "CaseWorker, Supervisor")]

// Policy-based authorization in MediatR pipeline
public class AuthorizationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuthorizationService _authService;
    private readonly IUserContext _userContext;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requirement = new CaseAccessRequirement(request);
        var result = await _authService.AuthorizeAsync(
            _userContext.User, request, requirement);
            
        if (!result.Succeeded)
            throw new UnauthorizedAccessException();
            
        return await next();
    }
}
```

### 3. Accessibility

**Requirements:**
- WCAG 2.1 AA compliance for Blazor UI
- Semantic HTML elements in Razor components
- ARIA labels for interactive elements
- Keyboard navigation support
- Screen reader compatibility

**Code Patterns:**
```razor
@* ✅ GOOD: Accessible Blazor component *@
<div>
    <label for="caseNumber" class="form-label">
        Case Number <span class="required" aria-label="required">*</span>
    </label>
    <input id="caseNumber" 
           type="text" 
           class="form-control" 
           @bind="CaseNumber"
           aria-describedby="caseNumberHelp"
           required />
    <div id="caseNumberHelp" class="form-text">
        Format: CASE-123456
    </div>
    <ValidationMessage For="() => CaseNumber" />
</div>

@* ❌ BAD: Missing accessibility attributes *@
<input type="text" @bind="CaseNumber" />
```

### 4. Maintainability

**Requirements:**
- Clear separation of concerns
- Explicit dependencies (constructor injection)
- Meaningful names (no abbreviations)
- XML documentation on public APIs
- Unit tests for domain logic

**Code Patterns:**
```csharp
/// <summary>
/// Creates a new case in the ATLAS system.
/// </summary>
/// <param name="command">The case creation details.</param>
/// <returns>The unique identifier of the created case.</returns>
/// <exception cref="ValidationException">Thrown when command validation fails.</exception>
public async Task<Result<Guid>> Handle(
    CreateCaseCommand command, 
    CancellationToken cancellationToken)
{
    // Implementation...
}
```

<!--
SECTION PURPOSE: Provide concrete examples of good vs bad patterns
PROMPTING TECHNIQUES: Visual checkmarks, side-by-side comparisons
-->

## Code Examples

### ✅ Good ATLAS Code

```csharp
// Domain Aggregate with business rules
namespace Atlas.Domain.Cases;

public class Case : AggregateRoot
{
    public CaseNumber CaseNumber { get; private set; }
    public Client Client { get; private set; }
    public CaseStatus Status { get; private set; }
    private readonly List<CaseNote> _notes = new();
    public IReadOnlyList<CaseNote> Notes => _notes.AsReadOnly();
    
    private Case() { } // EF Core
    
    public static Case Create(CaseNumber caseNumber, Client client)
    {
        if (client is null)
            throw new DomainException("Client cannot be null");
            
        var @case = new Case
        {
            Id = Guid.NewGuid(),
            CaseNumber = caseNumber ?? throw new ArgumentNullException(nameof(caseNumber)),
            Client = client,
            Status = CaseStatus.New
        };
        
        @case.RaiseDomainEvent(new CaseCreatedEvent(@case.Id, caseNumber.Value));
        return @case;
    }
    
    public Result AddNote(string content, string authorId)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure("Note content cannot be empty");
            
        if (content.Length > 5000)
            return Result.Failure("Note content exceeds 5000 character limit");
            
        var note = CaseNote.Create(content, authorId, this);
        _notes.Add(note);
        
        RaiseDomainEvent(new CaseNoteAddedEvent(Id, note.Id));
        return Result.Success();
    }
}
```

### ❌ Bad ATLAS Code

```csharp
// Anemic domain model with logic in service
namespace Atlas.Domain.Cases;

public class Case  // Just a data container
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } // Primitive obsession
    public string ClientName { get; set; }
    public int Status { get; set; } // Magic numbers
}

// Logic scattered in service
public class CaseService
{
    public void AddNote(Guid caseId, string content)
    {
        var @case = _repository.GetById(caseId);
        @case.Notes.Add(new Note { Content = content }); // No validation
        _repository.Update(@case);
        // No audit, no domain event
    }
}
```

<!--
SECTION PURPOSE: Reference related documentation
PROMPTING TECHNIQUES: Cross-references, hierarchical organization
-->

## Related Instructions

- **Backend**: `.github/instructions/backend.instructions.md` - General .NET backend patterns
- **Frontend**: `.github/instructions/frontend.instructions.md` - Blazor-specific guidance
- **BDD Tests**: `.github/instructions/bdd-tests.instructions.md` - Behavior-driven tests

<!--
SECTION PURPOSE: Decision framework for AI assistants
PROMPTING TECHNIQUES: Checklist format, gate-based validation
-->

## Decision Framework for AI Assistants

Before generating ATLAS code, verify:

- [ ] Does domain code have zero infrastructure dependencies?
- [ ] Are business rules in aggregates, not services?
- [ ] Are value objects used for domain concepts?
- [ ] Is state change auditable via domain events?
- [ ] Does code prioritize auditability, security, accessibility?
- [ ] Are Blazor components WCAG 2.1 AA compliant?
- [ ] Is CQRS pattern followed (commands vs queries)?
- [ ] Are MediatR pipelines used for cross-cutting concerns?

<!-- © Capgemini 2026 -->