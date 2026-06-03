---
title: "ADR-005: Select Blazor Server for Web UI"
status: "Accepted"
date: "2026-06-03"
authors: "David (Product Owner), Engineering Team"
tags: ["frontend", "blazor", "ui"]
supersedes: ""
superseded_by: ""
---

# ADR-005: Select Blazor Server for Web UI

## Status

### Accepted

## Context

ATLAS requires a web-based user interface for citizens, permit officers, and administrators. The PRD specifies (Constraint C-02): "Frontend must use Blazor (Server or WebAssembly TBD based on performance testing)". We need to decide which Blazor hosting model to use for the MVP launch (October 15, 2026).

Key requirements driving this decision:

1. **Real-time updates** - Officers need immediate visibility when new applications are submitted (PRD F-09)
2. **Rich interactivity** - Form validation, dynamic fields, document upload progress (PRD F-02, F-03)
3. **Government network compatibility** - Users may access from municipal offices with varying bandwidth
4. **Development velocity** - Team is already using .NET 9 (PRD C-01), Blazor leverages existing C# skills
5. **Accessibility compliance** - Must meet WCAG 2.1 AA (PRD NFR-05)

Alternative UI frameworks considered:

- **React with TypeScript** - Popular but requires JavaScript expertise, separate from .NET backend
- **Angular** - Full-featured but steep learning curve, separate ecosystem from .NET
- **ASP.NET Core MVC with Razor Pages** - Traditional approach, less interactive, more page refreshes
- **Blazor WebAssembly** - Client-side Blazor running in browser via WebAssembly

## Decision

We will use **Blazor Server** as the hosting model for the ATLAS MVP.

### Blazor Server Architecture

```text
┌─────────────────────────────────────────────────┐
│                    Browser                      │
│  (HTML + JavaScript SignalR Client)             │
│                                                 │
│  Sends UI events (clicks, form changes)         │
│  Receives UI updates (DOM diffs)                │
└─────────────────┬───────────────────────────────┘
                  │ SignalR WebSocket
                  │
┌─────────────────▼───────────────────────────────┐
│              ASP.NET Core Server                │
│                                                 │
│  ┌────────────────────────────────────────┐     │
│  │     Blazor Server Circuit              │     │
│  │  (Maintains component state in memory) │     │
│  │                                        │     │
│  │  - Renders UI on server                │     │
│  │  - Processes events on server          │     │
│  │  - Sends DOM updates via SignalR       │     │
│  └────────────────────────────────────────┘     │
│                                                 │
│  ┌────────────────────────────────────────┐     │
│  │     Atlas Application Layer            │     │
│  │  (CQRS Commands/Queries via MediatR)   │     │
│  └────────────────────────────────────────┘     │
└─────────────────────────────────────────────────┘
```

### Key Characteristics

- **Server-side rendering**: UI logic executes on the server, not in the browser
- **SignalR connection**: Real-time bidirectional communication between browser and server
- **Circuit state**: Each user's component state is maintained in server memory
- **Minimal client payload**: Browser downloads only HTML/JS, no .NET runtime

## Consequences

### Positive

1. **Real-time updates** - SignalR enables instant UI updates when data changes (officer dashboard refreshes automatically)
2. **Smaller initial payload** - No need to download .NET WebAssembly runtime (~2MB), faster initial page load
3. **.NET 9 on server** - Full access to .NET APIs, debugging, and performance profiling on server
4. **Simpler deployment** - No client-side WebAssembly constraints, standard ASP.NET Core hosting
5. **Development velocity** - Single language (C#) across frontend and backend, shared models/DTOs
6. **Accessibility** - Blazor Server generates standard HTML, compatible with screen readers (WCAG 2.1 AA)

### Negative

1. **Server affinity required** - Each user's circuit state is on a specific server instance (challenging for scale-out)
2. **Network latency sensitivity** - UI interactions require round-trip to server (impact on user experience)
3. **Connection reliability** - SignalR disconnections require circuit reconnection logic
4. **Server memory usage** - Each active user's component state consumes server memory
5. **Scalability ceiling** - Practical limit of ~500 concurrent users per server (acceptable for MVP per PRD)

### Mitigations

- **Azure App Service** - Use multiple instances with sticky sessions (ARR affinity) for MVP
- **Connection resilience** - Implement SignalR automatic reconnection with user notification
- **Memory management** - Configure circuit timeout (default 3 minutes) to release inactive state
- **Performance monitoring** - Use Application Insights to track circuit count and memory usage
- **Future migration path** - Blazor component code is mostly identical between Server and WebAssembly

## Alternatives Considered

### Blazor WebAssembly

- **ALT-001**: **Description**: Client-side Blazor running .NET in browser via WebAssembly
- **ALT-002**: **Rejection Reason**: Larger initial download (~2MB .NET runtime), offline capability not needed for MVP, more complex deployment, PRD constraint C-02 allows either mode but Server is recommended for government intranet scenarios

### React with TypeScript

- **ALT-003**: **Description**: Popular SPA framework with rich ecosystem
- **ALT-004**: **Rejection Reason**: Requires JavaScript/TypeScript expertise separate from .NET backend, breaks Constraint C-02 (must use Blazor), increases learning curve for .NET team

### ASP.NET Core MVC with Razor Pages

- **ALT-005**: **Description**: Traditional server-rendered pages with form posts
- **ALT-006**: **Rejection Reason**: Less interactive (full page refreshes), harder to implement real-time updates, doesn't meet modern UX expectations for permit officers

## Implementation Notes

- **IMP-001**: Configure SignalR with Azure SignalR Service for scalable connection management (post-MVP)
- **IMP-002**: Implement circuit reconnection UI with user-friendly "Reconnecting..." indicator
- **IMP-003**: Use `PreRenderMode.Server` for faster initial page load (server-rendered HTML, then interactive)
- **IMP-004**: Structure components with code-behind files (.razor.cs) for testability
- **IMP-005**: Leverage MediatR (ADR-002) in Blazor components for CQRS commands/queries
- **IMP-006**: Implement per-user authorization with Microsoft Entra ID (ADR-008) and role-based UI rendering

## Compliance with Requirements

| Requirement | How Blazor Server Addresses It |
| ----------- | ----------------------------- |
| PRD C-02: Blazor frontend | ✅ Uses Blazor (Server mode selected) |
| PRD F-09: Officer dashboard | ✅ Real-time updates via SignalR |
| PRD F-03: Document upload | ✅ Interactive progress indicator via SignalR |
| PRD NFR-05: WCAG 2.1 AA | ✅ Standard HTML output, screen reader compatible |
| PRD Scale: 500 concurrent users | ✅ Acceptable for MVP with proper App Service sizing |
| ADR-001: Clean Architecture | ✅ Blazor is Presentation layer, calls Application layer via MediatR |

## References

- **REF-001**: [ADR-001: Clean Architecture](adr-001-clean-architecture.md)
- **REF-002**: [ADR-002: CQRS with MediatR](adr-002-cqrs-mediatr.md)
- **REF-003**: [ADR-008: Microsoft Entra ID Authentication](adr-008-microsoft-entra-id.md)
- **REF-004**: [ATLAS PRD - Technology Stack](../PRDs/atlas-mvp-prd.md#technology-stack)
- **REF-005**: [Blazor Server vs WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models)
- **REF-006**: [SignalR in Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr)
