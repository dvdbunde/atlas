---
title: "ADR-012: Generated API Layer with NSwag"
status: "Accepted"
date: "2026-06-08"
authors: "Developer"
tags: ["architecture", "api", "code-generation", "nswag"]
supersedes: ""
superseded_by: ""
---

# ADR-012: Generated API Layer with NSwag

## Status

### Accepted

## Context**

The ATLAS API previously used manually maintained controllers that:

- Required manual synchronization between OpenAPI contract (tlas-api.yaml) and controller code
- Mixed concerns: routing, authorization, MediatR dispatch, and response mapping in single files
- Made it easy for controllers to drift from the contract
- Required manual updates when the API contract changed

Phase 1-3 established the OpenAPI contract as the source of truth and set up build-time code generation. Phase 4 implements the transition to generated controllers.

## Decision**

Replace manually maintained controllers with NSwag-generated controllers using:

1. **Generated controllers**: NSwag generates controller stubs from tlas-api.yaml into Controllers/Generated/GeneratedControllers.g.cs
2. **Partial class adapters**: Custom code lives in Controllers/Partial/*.partial.cs files that extend the generated partial classes
3. **DTO mapping layer**: Explicit mapping between NSwag-generated *Response types and Application layer*Dto types in Contracts/Generated/DtoMappingExtensions.cs
4. **MediatR integration**: Adapters dispatch commands/queries via IMediator, keeping business logic in the Application layer
5. **Authorization conventions**: Applied via ASP.NET Core convention-based policy in Program.cs (not attributes on generated code)

## Consequences**

### Positive

- **POS-001**: **Single source of truth**: OpenAPI contract (tlas-api.yaml) drives the API surface - routes, parameters, response types, and operation IDs
- **POS-002**: **Regeneration-safe**: Custom code lives outside generated files in Partial/ folder, surviving NSwag re-runs
- **POS-003**: **Clear separation of concerns**: Generated code handles routing, adapters handle MediatR dispatch and mapping
- **POS-004**: **Explicit contract boundaries**: *Response types (NSwag) vs*Dto types (Application) make the API contract boundary visible
- **POS-005**: **Preserved architecture**: MediatR, validation pipeline, and CQRS pattern remain unchanged in Application layer

### Negative

- **NEG-001**: **Mapping overhead**: Explicit mapping between *Response and*Dto types required (e.g., DateTimeOffset? ↔ DateTime?, Uri ↔ string)
- **NEG-002**: **Type conversion complexity**: ReviewResponseDecision enum (NSwag) vs int (Application) requires explicit conversion
- **NEG-003**: **Learning curve**: Developers must understand partial class pattern and which code goes where

## Alternatives Considered**

### Keep Manual Controllers

- **ALT-001**: **Description**: Continue maintaining controllers manually with careful contract synchronization
- **ALT-002**: **Rejection Reason**: High maintenance overhead, easy to drift from contract, contradicts Phase 1-3 decisions

### Use NSwag Client Generation Only

- **ALT-003**: **Description**: Generate only DTOs from OpenAPI, keep manual controllers
- **ALT-004**: **Rejection Reason**: Doesn't solve the controller-to-contract synchronization problem

### Use Different Code Generator (e.g., Refit, AutoRest)

- **ALT-005**: **Description**: Use alternative code generation tools
- **ALT-006**: **Rejection Reason**: NSwag already integrated, supports ASP.NET Core controllers with SystemTextJson

## Implementation Notes**

- **IMP-001**: **File structure**:
  - Controllers/Generated/ - NSwag-generated controllers (regeneration-safe, don't edit)
  - Controllers/Partial/ - Custom adapters implementing generated interfaces (edit these)
  - Contracts/Generated/ - NSwag-generated DTOs (*Response types) and mapping extensions

- **IMP-002**: **DTO naming convention**:
  - NSwag generates: ApplicationSummaryResponse, PermitTypeResponse, etc. (in ATLAS.API.Contracts.Generated)
  - Application layer: ApplicationSummaryDto, PermitTypeDto, etc. (in ATLAS.Application.DTOs)
  - Mapping extensions in DtoMappingExtensions.cs handle conversions

- **IMP-003**: **Authorization**: Applied via convention in Program.cs:
  `csharp
  options.Conventions.Add(new GeneratedControllerAuthorizationConvention());
  `
  This applies [Authorize] policies based on controller name/interface (e.g., ApplicationsController → ["Citizen,Officer,Admin"])

- **IMP-004**: **MediatR integration**: Adapters inject IMediator and dispatch commands/queries, returning *Response types to match generated interfaces

- **IMP-005**: **Rollback strategy**: Keep old controller files in git history; revert commit if issues arise

## References**

- **REF-001**: [ADR-001: Clean Architecture](adr-001-clean-architecture.md)
- **REF-002**: [ADR-002: CQRS with MediatR](adr-002-cqrs-mediatr.md)
- **REF-003**: [OpenAPI Specification](../openapi/atlas-api.yaml)
- **REF-004**: [NSwag Documentation](https://github.com/RicoSuter/NSwag)
- **REF-005**: [Phase 4 Plan](../../memories/session/plan.md)
