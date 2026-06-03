# Product Requirements Document (PRD)

## ATLAS - Case Management & Permit Processing Platform MVP

### 0. Revision History

| Date | Author | Change Description |
| ---------- | ------ | ------------------ |
| 2026-06-02 | David (Product Owner) | Initial MVP draft |

---

### 1. Overview

**Problem Statement:**
Local governments currently manage permit applications using paper-based processes, disconnected spreadsheets, or legacy systems that lack integration. Citizens face long wait times, limited visibility into application status, and must submit physical documents in person. Permit officers struggle with manual review processes, lack of centralized notes, and no audit trail. Administrators have no efficient way to manage permit types or track system usage.

**Value Proposition:**
ATLAS (Automated Tracking & Licensing Application System) modernizes the permit processing workflow by providing a unified digital platform for all stakeholders. Citizens gain 24/7 online access to submit applications and track status. Permit officers receive a streamlined review interface with collaboration tools. Administrators gain configuration control and complete audit visibility. The platform reduces processing time, improves transparency, and ensures compliance through Azure-backed security and scalability.

---

### 2. Goals & Objectives

**Goals:**

- Digitize and streamline the end-to-end permit application process for local government
- Provide transparent, real-time status tracking for citizens
- Enable efficient review workflows for permit officers with audit compliance
- Establish a scalable, secure foundation for future government services

**Objectives:**

- Enable 100% digital permit application submission by MVP launch (Target: Q3 2026)
- Reduce permit processing time by 40% compared to manual processes (Baseline: 14 days average, Target: 8 days)
- Achieve 90% citizen satisfaction rate with status tracking transparency (Measure via post-approval survey)
- Ensure 100% audit trail coverage for all permit actions (Officer reviews, approvals, rejections)
- Support 500+ concurrent users on Azure infrastructure without performance degradation

---

### 3. Stakeholders

| Name/Group | Role/Responsibility | Contact/Notes |
| ---------------------- | ------------------- | ------------------- |
| Citizens | End users submitting permit applications | Public-facing portal users |
| Permit Officers | Review and process applications | Building dept, Planning dept, Zoning dept |
| Administrators | System configuration and oversight | Town/City Clerk, IT Admin |
| Product Owner | Requirements and prioritization | David (current document author) |
| Engineering Team | Implementation and technical delivery | .NET/Blazor developers |
| QA Team | Testing and validation | Manual + automated testing |

---

### 4. Specifications & Use Cases

#### User Stories

**Citizens:**

- As a citizen, I want to submit a permit application online so that I can apply without visiting the office in person
- As a citizen, I want to upload supporting documents (PDF, images) so that I can provide all required evidence digitally
- As a citizen, I want to track my application status in real-time so that I know where my application is in the review process

**Permit Officers:**

- As a permit officer, I want to review permit applications with all details in one place so that I can make informed decisions efficiently
- As a permit officer, I want to add internal notes to applications so that I can document my review observations
- As a permit officer, I want to approve applications with a single action so that I can process straightforward cases quickly
- As a permit officer, I want to reject applications with a reason so that citizens understand what corrections are needed

**Administrators:**

- As an administrator, I want to manage permit types (create, update, deactivate) so that the system reflects current municipal requirements
- As an administrator, I want to view a complete audit history so that I can investigate issues and ensure compliance

#### Use Cases

### UC1: Citizen Submits Permit Application

- Citizen logs into portal
- Selects permit type from active list
- Fills out application form
- Uploads required documents
- Submits application
- System generates confirmation number and sends email notification

### UC2: Permit Officer Reviews Application

- Officer logs into dashboard
- Views queue of pending applications
- Opens application details
- Reviews submitted documents
- Adds review notes
- Approves or rejects with comments

### UC3: Administrator Manages Permit Types

- Admin accesses configuration panel
- Creates new permit type with required fields
- Sets document requirements
- Activates/deactivates permit types
- Views audit log of all configuration changes

---

### 5. Functional Requirements

#### Citizens

| ID | Requirement | Priority |
| ---- | ------------- | ---------- |
| F-01 | Citizens can create a new permit application by selecting from active permit types | Must |
| F-02 | Citizens can fill out permit application forms with validation (required fields, data formats) | Must |
| F-03 | Citizens can upload supporting documents (PDF, JPG, PNG) up to 25MB per file | Must |
| F-04 | Citizens can view a list of their submitted applications with current status | Must |
| F-05 | Citizens can view detailed status history for each application (Submitted, Under Review, Approved, Rejected) | Must |
| F-06 | Citizens receive email notifications on status changes | Should |
| F-07 | Citizens can save draft applications and resume later | Should |
| F-08 | Citizens can download previously uploaded documents | Could |

#### Permit Officers

| ID | Requirement | Priority |
| ---- | ------------- | ---------- |
| F-09 | Officers can view a dashboard of pending applications assigned to them or their department | Must |
| F-10 | Officers can open application details including all form data and uploaded documents | Must |
| F-11 | Officers can add internal review notes to applications (not visible to citizens) | Must |
| F-12 | Officers can approve applications with digital signature/confirmation | Must |
| F-13 | Officers can reject applications with mandatory reason code and comments | Must |
| F-14 | Officers can filter and search applications by status, date range, permit type | Should |
| F-15 | Officers can request additional information from citizens (triggers status change) | Should |
| F-16 | Officers can view application history and previous officer notes | Could |

#### Administrators

| ID | Requirement | Priority |
| ---- | ------------- | ---------- |
| F-17 | Administrators can create new permit types with configurable fields and requirements | Must |
| F-18 | Administrators can edit existing permit type configurations (fields, requirements, fees) | Must |
| F-19 | Administrators can activate/deactivate permit types (soft delete) | Must |
| F-20 | Administrators can view a complete audit history of all system actions (user actions, configuration changes) | Must |
| F-21 | Administrators can manage user accounts and assign roles (Citizen, Officer, Admin) | Should |
| F-22 | Administrators can configure system-wide settings (notification templates, document size limits) | Should |
| F-23 | Administrators can export audit data to CSV/Excel | Could |

#### Acceptance Criteria

**F-01 Acceptance Criteria:**

- Citizen can see only active permit types in dropdown
- Selecting permit type loads corresponding form fields
- System validates permit type is active before allowing submission

**F-03 Acceptance Criteria:**

- System accepts PDF, JPG, PNG formats
- System rejects files >25MB with clear error message
- Uploaded files are stored in Azure Blob Storage
- System displays upload progress indicator

**F-05 Acceptance Criteria:**

- Status history shows timestamp for each status change
- Status changes trigger email notifications (F-06)
- Citizens cannot modify applications after "Under Review" status

**F-12 Acceptance Criteria:**

- Approval action requires confirmation dialog
- System records officer ID, timestamp, and any notes
- Application status changes to "Approved"
- Citizen receives approval notification with details

**F-20 Acceptance Criteria:**

- Audit log captures: user ID, action type, timestamp, affected record ID, before/after values
- Audit log is immutable (no delete/update)
- Administrators can filter audit log by user, action type, date range
- Audit log retention: 7 years (compliance requirement)

---

### 6. Out of Scope

The following items are **explicitly out of scope** for the MVP release:

- **Payment processing integration** — Permit fees will be handled offline (in person/check) for MVP
- **Digital signatures from citizens** — Physical signature will be required at permit pickup
- **Mobile native apps** — MVP is web-responsive only (Blazor Server)
- **Integration with third-party systems** (GIS, property records, state databases)
- **Bulk application imports** — All applications must be submitted through the portal
- **Advanced reporting/analytics dashboard** — Only audit history and basic status counts for MVP
- **Multi-language support** — English only for MVP
- **Public API for external systems** — Internal use only for MVP
- **Workflow customization** (custom approval chains) — Linear review process only (Submit → Review → Approve/Reject)
- **Document versioning** — Latest upload replaces previous for same document type

---

### 7. Non-Functional Requirements

#### Performance

- **NFR-01**: Page load time ≤ 2 seconds for all dashboard views (measured via Azure Application Insights)
- **NFR-02**: Document upload of 25MB file completes within 30 seconds on 50Mbps connection
- **NFR-03**: System supports 500 concurrent users with <5% error rate during peak hours (9-11 AM, 2-4 PM)

#### Security

- **NFR-04**: All data in transit encrypted via TLS 1.3
- **NFR-05**: All data at rest encrypted via Azure Storage Service Encryption (Azure SQL TDE, Blob Storage encryption)
- **NFR-06**: Authentication via Microsoft Entra ID (Azure AD) with MFA support for government employees
- **NFR-07**: Role-based access control (RBAC) enforced at API and UI levels
- **NFR-08**: Citizens can only access their own applications (row-level security)
- **NFR-09**: Audit logs are immutable and retained for 7 years (compliance requirement)

#### Usability

- **NFR-10**: Responsive design supporting desktop, tablet, and mobile browsers (Chrome, Edge, Firefox)
- **NFR-11**: WCAG 2.1 AA compliance for accessibility (required for government services)
- **NFR-12**: Maximum 3 clicks to reach any primary function from dashboard
- **NFR-13**: Clear error messages with actionable guidance (no system error codes exposed to users)

#### Reliability

- **NFR-14**: System uptime ≥ 99.9% (excluding planned maintenance windows)
- **NFR-15**: Automated daily backups of Azure SQL database with 30-day retention
- **NFR-16**: Geo-redundant storage (GRS) for Azure Blob Storage (documents)
- **NFR-17**: Maximum data loss of 1 hour (RPO) and recovery time of 4 hours (RTO) for disaster recovery

#### Scalability

- **NFR-18**: Azure SQL Database configured with auto-scaling (Serverless tier for MVP)
- **NFR-19**: Azure Blob Storage supports unlimited document growth (pay-as-you-grow model)
- **NFR-20**: Blazor Server configured with Azure App Service auto-scaling (scale-out based on CPU/memory)

---

### 8. Success Metrics

#### Metric 1: Permit Application Completion Rate

- **Baseline**: 0% (new system, no historical data)
- **Target**: 85% of started applications reach "Submitted" status
- **Timeframe**: By end of Q4 2026 (3 months post-launch)
- **Datasource**: Application lifecycle events in Azure SQL (tracked via Application Insights custom events)
- **Owner**: Product Owner
- **Guardrails**: Average application completion time ≤ 15 minutes; Error rate on submissions ≤ 2%

#### Metric 2: Permit Processing Time (Average)

- **Baseline**: 14 days (current manual process, measured Q1 2026)
- **Target**: 8 days (40% reduction)
- **Timeframe**: By end of Q4 2026
- **Datasource**: Application status timestamps in Azure SQL (`SubmittedDate` to `DecisionDate`)
- **Owner**: Permit Department Lead
- **Guardrails**: 95% of applications processed within 14 days; Officer workload balanced (no officer with >20 pending applications)

#### Metric 3: Citizen Satisfaction Score (Status Transparency)

- **Baseline**: N/A (new feature, no baseline)
- **Target**: 90% satisfaction rate with status tracking feature
- **Timeframe**: By end of Q4 2026 (measured via post-approval survey)
- **Datasource**: Survey responses collected via email link after permit decision
- **Owner**: Product Owner
- **Guardrails**: Survey response rate ≥ 30%; No critical usability issues reported (tracked via UserVoice or feedback form)

#### Metric 4: System Uptime (SLA Compliance)

- **Baseline**: N/A (new system)
- **Target**: ≥ 99.9% uptime (excluding planned maintenance)
- **Timeframe**: Continuous; measured monthly
- **Datasource**: Azure Monitor uptime probes (ping tests every 5 minutes)
- **Owner**: Engineering Team / SRE
- **Guardrails**: Mean Time to Recovery (MTTR) ≤ 1 hour; Maximum 1 planned maintenance window per month (max 2 hours)

#### Metric 5: Document Upload Success Rate

- **Baseline**: N/A (new feature)
- **Target**: 98% successful upload rate (no errors)
- **Timeframe**: By end of Q3 2026 (1 month post-launch)
- **Datasource**: Azure Blob Storage upload success/failure events (tracked via Application Insights)
- **Owner**: Engineering Team
- **Guardrails**: Average upload time ≤ 30 seconds for 25MB file; Storage cost per document ≤ $0.10/month

---

### 9. Constraints & Assumptions

#### Technical Constraints

- **C-01**: Application must be built with **.NET 9** and **ASP.NET Core** (framework requirement)
- **C-02**: Frontend must use **Blazor** (Server or WebAssembly TBD based on performance testing)
- **C-03**: Database must use **Azure SQL Database** (Serverless tier for cost optimization)
- **C-04**: Document storage must use **Azure Blob Storage** (with CDN for performance if needed)
- **C-05**: Authentication must integrate with **Microsoft Entra ID** (Azure AD) for government employees; citizens may use local accounts (MVP) or Entra External ID (future)
- **C-06**: Application must be hosted on **Azure App Service** (Windows or Linux plan TBD)

#### Business Constraints

- **C-07**: MVP must launch by **Q3 2026** (fiscal year budget cycle)
- **C-08**: System must comply with **government data sovereignty** requirements (data must remain in West Europe region)
- **C-09**: No budget for third-party SaaS integrations (payment, e-signature) in MVP
- **C-10**: Staff training budget limited to 2 days maximum (simple UI required)

#### Assumptions

- **A-01**: Citizens have access to internet and basic digital literacy (public library kiosks available as backup)
- **A-02**: Permit officers have government-issued laptops with modern browsers
- **A-03**: Azure subscription and resource quotas are approved and available
- **A-04**: IT department will support Entra ID configuration and user provisioning
- **A-05**: Current permit types and requirements are well-documented and stable (no major regulatory changes during MVP development)
- **A-06**: Email delivery service (SendGrid or Azure Communication Services) is approved for notifications

---

### 10. Timeline & Milestones

#### High-Level Timeline (MVP)

| Milestone | Target Date | Dependencies |
| ----------- | ------------- | -------------- |
| **Requirements Finalized** | June 15, 2026 | PRD approval (this document) |
| **Technical Spike & Architecture** | June 30, 2026 | Azure subscription access, Entra ID setup |
| **UI/UX Mockups Approved** | July 15, 2026 | Stakeholder review (Citizens, Officers, Admins) |
| **Backend API & Database Schema Complete** | August 15, 2026 | Architecture approval |
| **Frontend Blazor Components Complete** | September 1, 2026 | Backend API ready |
| **Integration Testing Complete** | September 15, 2026 | Frontend + Backend complete |
| **UAT with Stakeholders** | September 30, 2026 | Testing complete, training materials ready |
| **Go-Live (MVP Launch)** | October 15, 2026 | UAT sign-off, production Azure resources deployed |
| **Post-Launch Review** | November 15, 2026 | 30 days production metrics |

#### Critical Path

1. Azure infrastructure provisioning → Backend development → Frontend development → Integration testing
2. Entra ID configuration must complete before authentication testing (July 1, 2026)
3. Database schema must be finalized before backend API development starts (August 1, 2026)

---

### 11. Risks & Mitigations

| Risk | Impact | Probability | Mitigation Strategy |
| ------ | --------- | ------------- | --------------------- |
| **Azure subscription/quota delays** | High (blocks development) | Medium | Start Azure resource provisioning immediately; have backup subscription ready |
| **Blazor performance issues with large forms** | Medium (UX degradation) | Medium | Prototype complex forms early (June); consider Blazor Server vs WASM tradeoffs |
| **Entra ID configuration complexity** | High (officer access blocked) | Medium | Engage IT early; document configuration steps; have local auth fallback for testing |
| **Citizens struggle with digital submission** | High (low adoption) | Medium | Provide clear UI guidance; create help documentation; offer in-person assistance at office |
| **Document upload failures (large files, slow internet)** | Medium (citizen frustration) | High | Implement chunked upload; show clear progress; allow multiple small files instead of one large file |
| **Scope creep (officers request additional features)** | Medium (timeline slip) | High | Strict MVP scope enforcement; use "Out of Scope" section; defer to future enhancements |
| **Data migration from legacy system** | Low (MVP is new permits only) | Low | MVP only handles new applications; legacy data remains in read-only archive |
| **Azure cost overruns** | Medium (budget exceeded) | Low | Use Azure Cost Management alerts; Serverless tier for SQL; monitor Blob Storage growth |

---

### 12. Open Questions

1. **Q1**: Should citizens use local accounts (username/password) or Entra External ID for authentication?
   - *Impact*: Affects development effort and security compliance
   - *Decision needed by*: June 15, 2026

2. **Q2**: What is the approval workflow for permits that require multiple officer reviews (e.g., Building + Zoning)?
   - *Impact*: May require workflow engine vs. simple linear approval
   - *Decision needed by*: June 30, 2026 (before architecture finalization)

3. **Q3**: Should rejected applications allow citizens to resubmit with corrections, or must they start a new application?
   - *Impact*: UX flow and database schema (versioning vs. new record)
   - *Decision needed by*: July 15, 2026 (before backend development)

4. **Q4**: What email service should be used for notifications (SendGrid, Azure Communication Services, SMTP relay)?
   - *Impact*: Cost, deliverability, configuration effort
   - *Decision needed by*: July 1, 2026 (before integration testing)

5. **Q5**: Should the MVP include a public-facing permit status lookup (no login required, just confirmation number)?
   - *Impact*: Security considerations, citizen convenience
   - *Decision needed by*: June 30, 2026

---

### 13. References & Related Documents

- **ATLAS Architecture Document**: `docs/architecture/atlas-architecture.md` (to be created)
- **ATLAS Database Schema**: `docs/design/atlas-database-schema.md` (to be created)
- **ATLAS UI/UX Mockups**: `docs/design/figma-link-here` (to be created)
- **Azure Infrastructure (Bicep/Terraform)**: `infra/` directory (to be created)
- **.NET 9 Documentation**: <https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9>
- **Blazor Documentation**: <https://learn.microsoft.com/en-us/aspnet/core/blazor/>
- **Azure SQL Database Documentation**: <https://learn.microsoft.com/en-us/azure/azure-sql/>
- **Azure Blob Storage Documentation**: <https://learn.microsoft.com/en-us/azure/storage/blobs/>
- **Microsoft Entra ID Documentation**: <https://learn.microsoft.com/en-us/entra/>
- **PRD Template**: `docs/PRDs/prd-template.md` (this document follows that template)
- **Success Metrics Snippet**: `.github/prompts/snippets/prd-success-metrics.snippet.md`

---

### 14. Future Enhancements (Post-MVP)

The following features are **out of scope for MVP** but identified for future releases:

1. **Online Payment Integration** — Accept credit card/debit payments via Stripe or Azure Payment HSM
2. **Digital Signatures** — Citizens sign applications digitally (DocuSign integration or Azure AD ES)
3. **Mobile Native Apps** — iOS and Android apps for citizens and officers
4. **GIS Integration** — Auto-populate property details from municipal GIS system
5. **Bulk Import Tool** — Allow officers to import multiple applications from spreadsheets
6. **Advanced Analytics Dashboard** — Visualize permit trends, processing times, officer performance
7. **Multi-Language Support** — French, Spanish, and other languages based on demographic analysis
8. **Public API** — Allow third-party systems (contractors, architects) to submit applications programmatically
9. **Custom Workflow Engine** — Configurable approval chains (e.g., Building → Zoning → Fire Marshal)
10. **Document Versioning** — Track multiple versions of uploaded documents with rollback capability
11. **AI-Powered Application Review** — Auto-check applications for completeness using ML models
12. **SMS Notifications** — Send status updates via SMS in addition to email
13. **Calendar Integration** — Schedule inspections and sync with officer calendars (Outlook/Google)
14. **Offline Mode** — Allow officers to review applications offline (sync when reconnected)

---
