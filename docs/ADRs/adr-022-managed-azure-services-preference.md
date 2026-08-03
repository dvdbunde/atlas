---
title: "ADR-022: Managed Azure Services over Self-Hosted Infrastructure"
status: "Accepted"
date: "2026-07-28"
authors: "Engineering Team"
tags: ["architecture", "azure", "paas", "managed-services", "milestone-9"]
supersedes: ""
superseded_by: ""
---

# ADR-022: Managed Azure Services over Self-Hosted Infrastructure

## Status

### Accepted

## Context

Milestone 9 provisions the full set of Azure resources required for production deployment. For each capability — compute, database, storage, secrets, monitoring — ATLAS must choose between a managed Azure Platform-as-a-Service (PaaS) offering and a self-hosted alternative on Infrastructure-as-a-Service (IaaS) or containerized infrastructure.

The project is a public-sector permit processing system with a small engineering team. The team's expertise is in application development, not infrastructure operations. The architectural decisions made in earlier milestones — Clean Architecture (ADR-001), Azure SQL Database and Blob Storage (ADR-003), Azure Key Vault (ADR-009), and Azure App Service (ADR-019) — already reflect a preference for managed services, but the general principle was never stated explicitly.

Without a guiding principle, each resource decision would be evaluated in isolation, potentially leading to an inconsistent mix of managed and self-managed approaches. Some team members might prefer self-hosting to reduce Azure costs, while others might prefer managed services to reduce operational burden. A consistent principle is needed to guide these trade-off decisions.

## Decision

Whenever practical, ATLAS prefers managed Azure Platform-as-a-Service offerings instead of self-hosted infrastructure. This applies to the following services in the current architecture:

- **Azure App Service** (ADR-019): Managed compute for ATLAS.API and ATLAS.Blazor.
- **Azure SQL Database**: Managed relational database. PaaS includes automated backups, patching, high availability, and built-in row-level security support.
- **Azure Blob Storage**: Managed object storage for permit documents. PaaS includes geo-redundancy, lifecycle management, and soft delete.
- **Azure Key Vault** (ADR-009): Managed secrets management. PaaS includes hardware security module (HSM) support, access logging, and automated certificate rotation.
- **Application Insights**: Managed application performance monitoring. PaaS includes telemetry ingestion, analysis, and alerting.

### Exception Criteria

A self-hosted alternative may be considered when:

1. The managed service does not support a required capability.
2. The cost of the managed service exceeds the total cost of ownership of a self-hosted alternative by a significant margin, validated through a cost analysis.
3. Compliance requirements mandate infrastructure-level isolation that managed services cannot provide.
4. The team has the operational capacity to manage the self-hosted alternative.

## Consequences

### Positive

- **Operational simplicity**: Managed services abstract infrastructure operations — patching, backup, replication, failover. The team does not need expertise in SQL Server administration, storage infrastructure, or secrets management.
- **Reduced maintenance burden**: No virtual machine patching, no operating system updates, no storage hardware monitoring. The platform team (Microsoft) handles infrastructure reliability.
- **Security**: Managed services include built-in security features (encryption at rest and in transit, network isolation, access auditing) that would require significant effort to implement correctly on self-hosted infrastructure.
- **Scalability**: Managed services scale independently. Azure SQL Database can scale DTUs without changing application code. Blob Storage scales automatically. Self-hosted alternatives require capacity planning and manual scaling.
- **Disaster recovery**: Managed services include built-in redundancy options (geo-replication, zone-redundant storage, automated backups). Self-hosted disaster recovery requires designing, implementing, and testing a custom solution.

### Negative

- **Azure vendor lock-in**: Managed services are Azure-specific and use proprietary APIs, authentication models, and management interfaces. Migrating to another cloud provider or on-premises would require significant rework. The lock-in is mitigated by Clean Architecture (ADR-001), which abstracts infrastructure behind interfaces at the Application and Domain layers.
- **Cost predictability**: PaaS pricing is consumption-based and can be difficult to forecast for variable workloads. Self-hosted infrastructure has predictable fixed costs. The trade-off is operational simplicity for cost predictability.
- **Limited control**: Managed services expose a subset of configuration options. Certain advanced features (SQL Server Agent jobs, CLR integration, custom storage tiers) may not be available or may require higher service tiers.
- **Shared responsibility**: Managed services do not eliminate all security responsibilities. The team remains responsible for application-level security, access control, and data classification.

## Alternatives Considered

### Infrastructure-as-a-Service (Virtual Machines)

- **Description**: Deploying SQL Server, application servers, and storage infrastructure on Azure Virtual Machines with full control over the operating system and software configuration.
- **Rejection Reason**: IaaS requires the team to manage operating system patching, database administration, storage configuration, high availability setup, and backup management. For a small team focused on application delivery, this operational burden is not justified. The cost of a skilled database administrator alone exceeds the PaaS premium.

### Self-Managed Virtual Machines

- **Description**: Running application servers and databases on self-managed VMs with manual configuration of load balancing, failover, and monitoring.
- **Rejection Reason**: Same as IaaS — operational burden is too high. Additionally, self-managed VMs lack the built-in security features (managed identity, network security groups, Azure Policy integration) that managed services provide.

### Container-First Hosting

- **Description**: Running all application components in containers orchestrated by Azure Kubernetes Service or Azure Container Apps, with self-managed stateful services.
- **Rejection Reason**: Container orchestration adds operational complexity without providing architectural benefits for the current application. ATLAS is a single Blazor Server application, not a microservices deployment. Containers would be appropriate if the architecture evolves toward independently deployable services.

## References

- ADR-001: Clean Architecture (infrastructure abstraction reduces the impact of managed service dependency)
- ADR-003: Azure SQL & Blob Storage (managed database and storage decisions)
- ADR-009: Azure Key Vault (managed secrets management decision)
- ADR-019: Azure App Service as the Primary Production Hosting Platform (managed compute decision)
