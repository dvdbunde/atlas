---
title: "ADR-010: Row-Level Security Strategy for Multi-Tenant Data Access"
status: "Proposed"
date: "2026-06-03"
authors: "David (Product Owner), Engineering Team"
tags: ["security", "database", "azure-sql", "rls"]
supersedes: ""
superseded_by: ""
---

# ADR-010: Row-Level Security Strategy for Multi-Tenant Data Access

## Status

### Proposed

## Context

ATLAS is a multi-user permit management system with distinct user roles (Citizens, Officers, Administrators). The system must enforce data access restrictions per **PRD NFR-08**: "Citizens can only access their own applications (row-level security)".

**Access Requirements:**

1. **Citizens** - Can only view/create applications where `CitizenId = current_user_id`
2. **Permit Officers** - Can view applications assigned to their department or assigned to them
3. **Administrators** - Can view all applications but actions are fully audited (F-20)

**Current State (MVP Planning):**

- PRD NFR-08 mentions "row-level security" but no technical design exists
- Domain model shows `Application.CitizenId` but no enforcement strategy
- No Azure SQL Row-Level Security (RLS) policy designed
- Unclear if enforcement should be at database layer (RLS) or application layer

Alternative approaches considered:

- **Application-Layer Filtering** - Add `WHERE CitizenId = @UserId` in all queries (simple, but prone to developer error)
- **Azure SQL Row-Level Security (RLS)** - Database-enforced security policies (robust, but requires careful setup)
- **Separate Schemas/Table per Tenant** - Overkill for ATLAS (not multi-tenant SaaS, just role-based)
- **Graph-Based Permissions** - Too complex for MVP (consider for Phase 2 with complex org structures)

## Decision

We will use a **hybrid approach**:

1. **MVP (Milestone 7)**: **Application-Layer Filtering** with repository pattern enforcement
2. **Phase 2 (Post-MVP)**: Migrate to **Azure SQL Row-Level Security (RLS)** for defense-in-depth

### MVP Strategy: Application-Layer Filtering (Milestone 7)

All data access goes through repository interfaces defined in Application layer. Repositories automatically filter by user context.

#### Repository Pattern Enforcement

```csharp
// Application Layer - Repository Interface
public interface IApplicationRepository
{
    Task<Application> GetByIdAsync(Guid id, Guid currentUserId, UserRole role);
    Task<IEnumerable<Application>> GetByCitizenIdAsync(Guid citizenId);
    Task<IEnumerable<Application>> GetPendingForOfficerAsync(Guid officerId, string department);
    Task<IEnumerable<Application>> GetAllForAdminAsync(); // Admin only
}

// Infrastructure Layer - Repository Implementation
public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public async Task<Application> GetByIdAsync(Guid id, Guid currentUserId, UserRole role)
    {
        var query = _context.Applications.Where(a => a.Id == id);
        
        // Enforce row-level access
        if (role == UserRole.Citizen)
        {
            query = query.Where(a => a.CitizenId == currentUserId);
        }
        else if (role == UserRole.Officer)
        {
            query = query.Where(a => a.AssignedOfficerId == currentUserId 
                                   || a.PermitType.Department == _currentUser.Department);
        }
        // Admin: no filter (can see all)
        
        var application = await query.FirstOrDefaultAsync();
        
        if (application == null)
            throw new UnauthorizedAccessException("User cannot access this application");
            
        return application;
    }
}
```

#### CQRS Query Handlers with Security Context

```csharp
// Application Layer - Query
public class GetApplicationByIdQuery : IRequest<ApplicationDto>
{
    public Guid ApplicationId { get; set; }
}

public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, ApplicationDto>
{
    private readonly IApplicationRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public async Task<ApplicationDto> Handle(GetApplicationByIdQuery request, CancellationToken ct)
    {
        var application = await _repository.GetByIdAsync(
            request.ApplicationId, 
            _currentUser.UserId, 
            _currentUser.Role
        );
        
        return _mapper.Map<ApplicationDto>(application);
    }
}
```

### Phase 2 Strategy: Azure SQL Row-Level Security (RLS)

For defense-in-depth and compliance auditing, implement database-enforced RLS policies.

#### RLS Policy Definition (SQL)

```sql
-- Create security policy function
CREATE FUNCTION dbo.ApplicationAccessPredicate(@CitizenId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS AccessResult
WHERE @CitizenId = CAST(SESSION_CONTEXT(N'UserId') AS uniqueidentifier)
   OR CAST(SESSION_CONTEXT(N'UserRole') AS nvarchar(50)) = 'Admin'
   OR (CAST(SESSION_CONTEXT(N'UserRole') AS nvarchar(50)) = 'Officer'
       AND EXISTS (SELECT 1 FROM Applications a 
                  WHERE a.Id = Applications.Id 
                  AND a.AssignedOfficerId = CAST(SESSION_CONTEXT(N'UserId') AS uniqueidentifier)));

-- Apply security policy
CREATE SECURITY POLICY dbo.ApplicationSecurityPolicy
ADD FILTER PREDICATE dbo.ApplicationAccessPredicate(CitizenId) ON dbo.Applications
WITH (STATE = ON);
```

#### Setting Session Context in .NET

```csharp
// Infrastructure Layer - DbContext
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Set session context for RLS
        var userId = _currentUser.UserId.ToString();
        var userRole = _currentUser.Role.ToString();
        
        await Database.ExecuteSqlRawAsync(
            "EXEC sp_set_session_context @key=N'UserId', @value=@userId",
            new SqlParameter("@userId", userId));
            
        await Database.ExecuteSqlRawAsync(
            "EXEC sp_set_session_context @key=N'UserRole', @value=@userRole",
            new SqlParameter("@userRole", userRole));
            
        return await base.SaveChangesAsync(ct);
    }
}
```

## Consequences

### Positive (MVP - Application-Layer Filtering)

1. **Simplicity** - No database schema changes required for MVP
2. **Testability** - Filtering logic can be unit-tested in repository tests
3. **Flexibility** - Can implement complex business rules (department-based access)
4. **Performance** - No additional database overhead for MVP

### Positive (Phase 2 - Azure SQL RLS)

1. **Defense-in-Depth** - Database enforces security even if application layer has bugs
2. **Compliance** - Meets strict public sector audit requirements
3. **Auditability** - RLS policy executions can be logged in Azure SQL Audit
4. **No Code Bypass** - Even direct database access (by DBAs) is restricted

### Negative (MVP - Application-Layer Filtering)

1. **Reliance on Developer Discipline** - Must remember to use repository methods with user context
2. **Potential Bypass** - Raw SQL queries could bypass filtering (mitigated by using repositories only)
3. **Not Defense-in-Depth** - If application layer is compromised, database is fully accessible

### Negative (Phase 2 - Azure SQL RLS)

1. **Complexity** - Additional database objects to manage and version (with EF Core migrations)
2. **Performance** - RLS predicate evaluated on every query (mitigated by indexing)
3. **Testing Overhead** - Integration tests must set session context correctly

### Mitigations (MVP)

- **Code Review Checklist**: Add "RLS enforcement" check to `docs/engineering/pull-request-guidelines.md`
- **Repository Pattern**: ALL data access must go through repositories (no DbContext directly in Blazor pages)
- **Unit Tests**: Test that repositories enforce user context filtering
- **CI/CD Check**: Add analyzer rule to detect direct DbContext usage outside repositories

### Mitigations (Phase 2)

- **EF Core Migrations**: Manage RLS objects as part of database migrations
- **Integration Tests**: Test RLS policies with different user contexts
- **Monitoring**: Enable Azure SQL Audit to log RLS policy executions

## Alternatives Considered

### Application-Layer Filtering Only

- **ALT-001**: **Description**: Rely only on repository pattern filtering, no database-level RLS
- **ALT-001**: **Rejection Reason**: Does not meet "row-level security" requirement in PRD NFR-08; no defense-in-depth for public sector compliance

### Azure SQL RLS for MVP

- **ALT-002**: **Description**: Implement RLS immediately in MVP
- **ALT-002**: **Rejection Reason**: Adds complexity to MVP delivery; application-layer filtering is sufficient for initial release with proper code review

### Separate Tables per Role

- **ALT-003**: **Description**: Duplicate tables for citizen/officer/admin access
- **ALT-003**: **Rejection Reason**: Data duplication; complex synchronization; not scalable

## References

- **PRD NFR-08**: "Citizens can only access their own applications (row-level security)"
- **PRD F-20**: Audit requirements (RLS policy executions can be audited)
- **ADR-003**: Azure SQL Database (RLS is a feature of Azure SQL)
- **ADR-004**: Domain Model (Application.CitizenId, Application.AssignedOfficerId)
- **plans/atlas-foundation-plan.md**: Milestone 7 (Application Review) - implement MVP strategy here

## Implementation Plan

### MVP (Milestone 7: Application Review)

1. Implement `ICurrentUserService` to access current user context from HttpContext
2. Update all repository interfaces to accept user context parameters
3. Implement filtering logic in `ApplicationRepository`
4. Add unit tests for repository filtering
5. Add "RLS enforcement" to PR review checklist

### Phase 2 (Post-MVP Backlog)

1. Design RLS predicate functions for Applications table
2. Create EF Core migration to add RLS objects
3. Implement session context setting in DbContext
4. Add integration tests with different user roles
5. Enable Azure SQL Audit for RLS policy logging
6. Document RLS strategy in `docs/engineering/` security guide

---

**Next Steps:**

1. Add MVP RLS strategy to `plans/atlas-foundation-plan.md` Milestone 7
2. Create `ICurrentUserService` interface in Application layer
3. Update repository implementations to enforce user context
4. Add "RLS enforcement" check to `docs/engineering/pull-request-guidelines.md`
