---
title: "ADR-019: Azure App Service as the Primary Production Hosting Platform"
status: "Accepted"
date: "2026-07-28"
authors: "Engineering Team"
tags: ["architecture", "hosting", "azure", "app-service", "milestone-9"]
supersedes: ""
superseded_by: ""
---

# ADR-019: Azure App Service as the Primary Production Hosting Platform

## Status

### Accepted

## Context

Milestone 9 introduces production cloud deployment on Microsoft Azure. ATLAS consists of an ASP.NET Core API (ATLAS.API), a Blazor Server web application (ATLAS.Blazor), and supporting infrastructure. A hosting platform must be selected that aligns with the existing architectural decisions: Clean Architecture (ADR-001), CQRS with MediatR (ADR-002), Blazor Server (ADR-005), and Microsoft Entra ID authentication (ADR-008).

The application is a stateful Blazor Server application with a companion REST API. Blazor Server requires a persistent SignalR connection between client and server, which imposes constraints on the hosting environment. The platform must support managed identity integration, deployment slots for zero-downtime releases, and autoscaling to handle variable citizen demand.

## Decision

ATLAS targets Azure App Service as its production hosting platform. The application is intentionally optimized for Azure Platform-as-a-Service (PaaS) rather than container orchestration.

Azure App Service provides the managed runtime environment required by ASP.NET Core and Blazor Server without the operational complexity of managing container orchestration. The platform natively supports the architectural patterns already in use: managed identity for Azure Key Vault and Azure SQL Database access, deployment slots for staging and production swaps, and built-in autoscaling for demand fluctuations.

## Consequences

### Positive

- **Operational simplicity**: App Service abstracts operating system, runtime, and web server management. The team focuses on application code, not infrastructure patching.
- **Managed runtime**: ASP.NET Core is a first-class runtime on App Service. Platform updates are applied automatically.
- **Deployment slots**: Staging and production slots enable zero-downtime deployments and instant rollback. Slot-swap validation catches environment misconfiguration before production traffic is served.
- **Managed identity integration**: System-assigned managed identities provide secure, credential-free access to Azure SQL Database, Azure Key Vault, and Azure Blob Storage without storing connection strings or secrets.
- **Autoscaling**: Scale-out and scale-in rules handle variable citizen demand patterns without manual intervention.
- **Blazor Server suitability**: App Service maintains the persistent SignalR connections required by Blazor Server. WebSocket support is built in and requires no additional configuration.
- **Lower operational overhead**: No cluster management, no container orchestration, no virtual machine maintenance.

### Negative

- **Limited container flexibility**: App Service supports containers, but the full container orchestration capabilities of Azure Kubernetes Service are unavailable. Applications requiring sidecar patterns or fine-grained container scheduling may outgrow App Service.
- **Shared infrastructure**: App Service runs on multi-tenant worker nodes. Dedicated compute isolation (required by some compliance regimes) requires the Isolated tier.
- **Platform constraints**: Certain infrastructure-level customizations (kernel-level changes, custom Windows features) are not available on App Service.

## Alternatives Considered

### Azure Container Apps

- **Description**: A serverless container platform built on Kubernetes, supporting microservices, event-driven architectures, and KEDA-based scaling.
- **Rejection Reason**: Container Apps would add container orchestration overhead without immediate benefit. ATLAS is a single monolithic Blazor Server application, not a microservices deployment. The additional abstraction layer would complicate managed identity configuration, deployment slot workflows, and SignalR connection management without providing any capability the application currently requires.

### Azure Kubernetes Service

- **Description**: A fully managed Kubernetes cluster for containerized applications, offering maximum flexibility in orchestration, scaling, and infrastructure control.
- **Rejection Reason**: AKS introduces significant operational overhead (cluster management, node pool maintenance, container image build pipeline, monitoring infrastructure) that is not justified for a single Blazor Server deployment. The SignalR backplane required for Blazor Server on Kubernetes would add further complexity. AKS is a future option if the architecture evolves toward microservices.

### IIS on Virtual Machines

- **Description**: Traditional deployment of ASP.NET Core applications on Internet Information Services running on Windows virtual machines.
- **Rejection Reason**: VM-based hosting requires manual patching, load balancer configuration, certificate management, and capacity planning. It provides none of the PaaS benefits (managed runtime, deployment slots, autoscaling, managed identity) and introduces significant operational burden for a team focused on application delivery.

## References

- ADR-001: Clean Architecture (layer separation supports App Service deployment model)
- ADR-005: Blazor Server (Blazor SignalR architecture is compatible with App Service)
- ADR-008: Microsoft Entra ID (managed identity integration with App Service)
- ADR-009: Azure Key Vault (App Service managed identity access pattern)
