---
title: "ADR-016: Transaction Pipeline via MediatR TransactionBehavior"
status: "Accepted"
date: "2026-06-29"
authors: "Engineering Team"
tags: ["architecture", "transactions", "mediatr", "pipeline", "cqrs", "ar-001"]
supersedes: ""
superseded_by: ""
---

# ADR-016: Transaction Pipeline via MediatR TransactionBehavior

## Status

### Accepted

## Context

Milestone 5 introduced the MediatR pipeline pattern with `ValidationBehavior` and `UserSynchronizationBehavior`, but each command handler was responsible for calling `SaveChangesAsync()` on `IUnitOfWork` independently. This led to two concerns:

1. **Inconsistent persistence**: Some handlers called `SaveChangesAsync()`; others relied on the caller to commit. There was no guarantee that a command handler would persist changes before returning.
2. **Query-side concern**: Query handlers occasionally had access to write operations through injected repositories, creating a risk that read operations could accidentally modify state.

Additionally, the `UserSynchronizationBehavior` committed user sync changes through `IIdentityResolver.SynchronizeUserAsync()` before the handler executed, creating two separate transaction boundaries per request.

A review of the codebase showed that most command handlers followed a pattern of: validate → execute → save → publish events. This pattern was repeated across handlers, indicating a cross-cutting concern suitable for pipeline-level enforcement.

## Decision

Introduce a `TransactionBehavior` as the innermost MediatR pipeline behavior, constrained to command types only.

### Pipeline Order

ValidationBehavior — validates input (all request types)
↓
UserSynchronizationBehavior — syncs Entra ID → Domain User (all authenticated requests)
↓
TransactionBehavior — commits IUnitOfWork (commands only)
↓
Command Handler — executes business logic

### Design Rules

1. **Commands only**: `TransactionBehavior` is constrained via `where TRequest : ICommand<TResponse>`. Queries (which implement `IRequest<T>` directly, not `ICommand<T>`) pass through without triggering a commit. This is enforced at compile time through the generic constraint.

2. **Single commit**: `SaveChangesAsync()` is called exactly once, after `next()` succeeds. If the handler throws an exception, no commit occurs. There is no `try/catch` — failures propagate naturally to the caller.

3. **No distributed transaction**: The behavior does not create an EF Core `IDbContextTransaction`. For single-database scenarios, `SaveChangesAsync()` wraps changes in an implicit database transaction, which is sufficient. A distributed transaction coordinator would be needed only if a single command wrote to multiple independent stores.

4. **UserSync outside transaction**: `UserSynchronizationBehavior` commits its changes independently before `TransactionBehavior` runs. This means the user sync changes and the handler changes are in separate transaction boundaries. If the handler fails after sync succeeds, the user sync changes remain committed.

### Marker Interface

A new `ICommand<TResponse>` marker interface was introduced:

```csharp
public interface ICommand<TResponse> : MediatR.IRequest<TResponse>
{
}

```

All commands implement `ICommand<T>`; all queries implement plain `IRequest<T>`. This enables type-level pipeline constraints without runtime checks.

Consequences
Positive
POS-001: SaveChangesAsync() is called exactly once per command, eliminating inconsistent persistence.
POS-002: Queries never commit, preventing accidental writes from read operations.
POS-003: New command handlers automatically get transaction management without boilerplate.
POS-004: The pipeline is self-documenting — the ordering and responsibility of each behavior is explicit.
POS-005: Failure isolation — a handler exception prevents partial commits.
Negative
NEG-001: User sync and handler commit are in separate transaction boundaries. If the handler fails after sync succeeds, the synced user changes remain committed (e.g., an updated LastLoginDate).
NEG-002: No IDbContextTransaction wrapping means the entire pipeline is not atomic. A future enhancement could move transaction orchestration to an outer behavior.
NEG-003: Handlers must still call _repository.UpdateAsync(entity) to mark entities as changed in the change tracker (though SaveChangesAsync is handled by the behavior).
Mitigations for Negative Consequences
NEG-001 is an accepted trade-off documented in UserSynchronizationBehavior source comments. The user sync is idempotent and updates are cosmetic (profile data, login timestamps). A failed handler is retried by the client, which triggers another sync on retry.
NEG-002 is acceptable for the current single-database deployment. A distributed transaction wrapper would add complexity without immediate benefit.
Alternatives Considered
Alternative 1: Handler-Level SaveChanges
Each handler calls SaveChangesAsync() directly. No pipeline behavior.

Rejected: Inconsistent application across handlers; some handlers forget to save; query handlers can inadvertently commit.
Alternative 2: Outer IDbContextTransaction Behavior
A behavior wraps the entire pipeline (including UserSynchronizationBehavior) in an IDbContextTransaction.

Rejected: Requires either (a) moving the transaction to Infrastructure (violating Clean Architecture dependency rules), or (b) adding EF Core as an Application-layer dependency. Also complicates the retry-on-conflict logic in IdentityResolver.
Alternative 3: Unit of Work Interceptor
An EF Core SaveChangesInterceptor automatically commits at the end of every request scope.

Rejected: Too implicit — developers cannot see where commits happen. Also commits during queries if not carefully scoped.
Compliance
All new commands MUST implement `ICommand<TResponse>`, not IRequest `<TResponse>`.
Command handlers MUST NOT call _unitOfWork.SaveChangesAsync() directly.
Query handlers MUST NOT call any method that modifies state.
UserSynchronizationBehavior MUST remain outside the transaction boundary (accepted trade-off).

---

## Deliverable 2: Summary of Each Completed Cleanup Task

| Task | Description | Files Affected |
| ------ | ------------- | ---------------- |
| **1** | Remove obsolete SubmitApplicationCommand workflow | Delete 1 source file, modify 2 source files, delete 1 test file, modify 1 test file, update OpenAPI spec |
| **2** | Remove BlobUrl from public DTOs | Modify 1 DTO file, modify 1 query handler, update OpenAPI spec, regenerate NSwag contracts, update 2 test files |
| **3** | Remove UpdateAsync/DeleteAsync from AuditLogRepository | Modify 1 interface, modify 1 repository implementation |
| **4** | Create ADR-016 documenting AR-001 | Create 1 new file `docs/ADRs/adr-016-transaction-behavior.md` |

---

## Deliverable 3: Migration / Breaking Change Notes

**No data migration required.** These changes are source-code only:

| Change | Breaking? | Mitigation |
| -------- | ----------- | ------------ |
| Remove POST /api/applications endpoint (v1) | **Yes** — any client using the old one-step submit endpoint will receive 404 | Clients must use the draft workflow: `POST /api/applications/draft` → PUT /api/applications/{id} → `POST /api/applications/{id}/submit` |
| Remove BlobUrl from `DocumentResponse` | **Yes** — any client consuming blobUrl from application detail responses will see it missing | The download endpoint `/api/documents/{documentId}/download` remains the only supported download mechanism |
| Remove UpdateAsync/DeleteAsync from AuditLogRepository | **No** — no callers exist in the codebase | Compile-time check: if any caller existed, the build would fail |

---

## Deliverable 4: Manual Validation

1. **Build verification**: Run `dotnet build` — there should be 0 errors, 0 warnings (excluding the [Obsolete] on DocumentType which was already present)
2. **Test execution**: Run `dotnet test` — all tests should pass
3. **Download endpoint**: Verify `/api/documents/{id}/download` still returns a 302 redirect with a SAS URI
4. **Draft workflow**: Verify `POST /api/applications/draft` → PUT /api/applications/{id} → `POST /api/applications/{id}/submit` is the only application creation path
5. **Audit log**: Verify audit logging for document uploads/downloads continues to function

---

## Deliverable 5: Test Results

After applying all changes, run:

```bash
dotnet test --configuration Release
