# Project Roadmap

Use this roadmap to communicate upcoming milestones and recently completed work. Keep it lightweight and updated as plans evolve.

## Status Badges

- Planned: ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- In Progress: ![In Progress](https://img.shields.io/badge/status-In%20Progress-blue)
- Done: ![Done](https://img.shields.io/badge/status-Done-brightgreen)

Usage: Add one badge per item to convey current status. Optionally include an owner badge.
Example: `Q4 2025 — Platform auth hardening` ![In Progress](https://img.shields.io/badge/status-In%20Progress-blue) ![Owner](https://img.shields.io/badge/owner-Platform%20Team-informational)

## Milestones (Upcoming)

### Q3 2026 — MVP Foundation & Core Features

**Goals**: Establish Clean Architecture, implement core domain model, and deliver citizen-facing permit submission.

**Key Deliverables**:

- M1: Solution Foundation (Clean Architecture, CI/CD, Bicep templates) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- M2: Domain Model (Entities, Aggregates, Value Objects, Domain Events) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- M3: Database Persistence (EF Core, Azure SQL, Repositories) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- M4: Authentication (Microsoft Entra ID) ![Complete](https://img.shields.io/badge/status-Complete-brightgreen)
- M5: Permit Submission (Citizen UI, Form Validation) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- M6: Document Upload (Azure Blob Storage, Drag-and-Drop) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- M7: Application Review (Officer Dashboard, Review Workflow) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- M8: Audit Logging (Immutable AuditLog, 7-year Retention) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- M9: Azure Deployment (App Service, Key Vault, CI/CD) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)

**Owners**: Engineering Team, Product Owner

---

### Q4 2026 — MVP Launch & Stabilization

**Goals**: Launch MVP to production, conduct UAT, and stabilize system.

**Key Deliverables**:

- Go-Live (MVP Launch) — October 15, 2026 ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Post-Launch Review & Bug Fixes ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- UAT with Stakeholders (Citizens, Officers, Admins) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)

**Owners**: Product Owner, Engineering Team, Permit Department Lead

---

### Q1 2027 — Phase 2: Enhanced Features

**Goals**: Add notifications, workflow engine, and advanced features based on MVP feedback.

**Key Deliverables**:

- Notification Service (Multi-channel: Email, SMS) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Workflow Engine (Custom Approval Chains) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Azure Service Bus Integration (Async Processing) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Reporting & Analytics Dashboard ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- OpenTelemetry Integration (Distributed Tracing) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)

**Owners**: Engineering Team

---

### Q2 2027 — Phase 3: Public Sector Compliance & Scale

**Goals**: Enhance security, compliance, and scalability for broader adoption.

**Key Deliverables**:

- Azure Key Vault CMK (Customer-Managed Keys) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Azure SQL Row-Level Security (Defense-in-Depth) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- Entra External ID (Citizen MFA) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- AKS Migration (Container Orchestration) ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)
- FedRAMP Compliance Documentation ![Planned](https://img.shields.io/badge/status-Planned-lightgrey)

**Owners**: Engineering Team, IT Department, Compliance Officer

---

## Milestones (Completed)

*No milestones completed yet — MVP foundation in progress.*

---

## References

- **Detailed Plan**: `plans/atlas-foundation-plan.md`
- **Product Requirements**: `docs/PRDs/atlas-mvp-prd.md`
- **Architecture Decisions**: `docs/ADRs/` (ADR-001 through ADR-010)
- **Future Enhancements**: `docs/design/08-extension-points.md`

<!-- © Capgemini 2025 -->
