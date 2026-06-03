---
title: "ADR-011: Data Lifecycle Management & Retention Policy"
status: "Proposed"
date: "2026-06-03"
authors: "David (Product Owner), Engineering Team"
tags: ["data", "retention", "compliance", "privacy"]
supersedes: ""
superseded_by: ""
---

# ADR-011: Data Lifecycle Management & Retention Policy

## Status

### Proposed

## Context

ATLAS is a public-sector application handling sensitive permit data and PII. The PRD specifies **7-year audit log retention** (F-20, NFR-09), but **no retention policy exists for other data types**.

**Data Types Requiring Retention Policy:**

1. **Audit Logs** — PRD F-20: 7 years (compliance requirement)
2. **Permit Applications** — Approved/Rejected applications (how long to retain?)
3. **Documents (Blob Storage)** — Uploaded files linked to applications
4. **User Accounts** — Citizen/Officer/Admin accounts (GDPR "right to be forgotten"?)
5. **Draft Applications** — Incomplete applications (auto-purge after inactivity?)

**Current State (MVP Planning):**

- ✅ Audit logs: 7-year retention specified (F-20)
- ❌ Applications: No retention policy defined
- ❌ Documents: No retention policy defined
- ❌ Users: No account deletion/purging policy
- ❌ Drafts: No auto-purge policy

**Compliance Requirements:**

- **Government Records Act** — Permit records must be retained for 7 years after final decision
- **GDPR/Privacy Laws** — Citizens have "right to be forgotten" (but government exemptions may apply)
- **Audit Compliance** — All actions must be traceable for 7 years

Alternative approaches considered:

- **Keep Everything Forever** — Simple but violates privacy laws; storage costs grow unbounded
- **Manual Purging** — Admin deletes old records manually (error-prone, not scalable)
- **Time-Based Retention Policies** — Azure Storage lifecycle management (automated, policy-driven)
- **Event-Driven Purging** — Azure Functions triggered by timer (custom logic, flexible)

## Decision

We will implement **tiered data retention policies** using a combination of:

1. **Azure Blob Storage Lifecycle Management** — For document retention
2. **Azure SQL Temporal Tables + Purge Job** — For application/data retention
3. **Audit Log Immutable Storage** — Separate table with no delete/update (7-year retention)

### Retention Policy Table

| Data Type | Retention Period | Storage Location | Purge Method | Notes |
|-----------|-------------------|-------------------|--------------|-------|
| **Audit Logs** | 7 years (mandatory) | Azure SQL (`AuditLog` table) | No delete (immutable) | Compliance requirement (F-20) |
| **Approved Applications** | 7 years after approval | Azure SQL + Blob Storage | Purge after 7 years | Government Records Act |
| **Rejected Applications** | 7 years after rejection | Azure SQL + Blob Storage | Purge after 7 years | Government Records Act |
| **Draft Applications** | 30 days of inactivity | Azure SQL + Blob Storage | Auto-purge after 30 days | Prevents DB bloat |
| **Documents (Approved/Rejected)** | 7 years (matches application) | Azure Blob Storage | Lifecycle policy move to Archive → Delete | Use Blob lifecycle management |
| **Documents (Draft)** | 30 days (matches draft) | Azure Blob Storage | Lifecycle policy delete | Auto-purge with draft |
| **User Accounts (Inactive)** | 3 years of inactivity | Azure SQL (`User` table) | Soft delete, PII anonymization | GDPR compliance (if applicable) |
| **System Configuration** | Indefinite | Azure SQL | No purge | Required for historical reporting |

### Implementation: Azure Blob Storage Lifecycle Management

```json
{
  "rules": [
    {
      "name": "purge-draft-documents",
      "enabled": true,
      "type": "Lifecycle",
      "definition": {
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["drafts/"]
        },
        "actions": {
          "baseBlob": {
            "delete": { "daysAfterModificationGreaterThan": 30 }
          }
        }
      }
    },
    {
      "name": "archive-old-documents",
      "enabled": true,
      "type": "Lifecycle",
      "definition": {
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["applications/"]
        },
        "actions": {
          "baseBlob": {
            "tierToArchive": { "daysAfterModificationGreaterThan": 365 },
            "delete": { "daysAfterModificationGreaterThan": 2555 }  // 7 years
          }
        }
      }
    }
  ]
}
```

### Implementation: Azure SQL Purge Job (Azure Function)

```csharp
// Azure Function (Timer Trigger - runs daily)
public class DataPurgeFunction
{
    private readonly ApplicationDbContext _context;
    private readonly BlobServiceClient _blobServiceClient;

    [FunctionName("DailyDataPurge")]
    public async Task Run([TimerTrigger("0 0 2 * * *")]TimerInfo myTimer)  // 2 AM daily
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-7);
        
        // Purge old approved/rejected applications
        var oldApplications = await _context.Applications
            .Where(a => a.Status == ApplicationStatus.Approved 
                       || a.Status == ApplicationStatus.Rejected)
            .Where(a => a.DecisionDate < cutoffDate)
            .ToListAsync();
        
        foreach (var app in oldApplications)
        {
            // Delete associated documents from Blob Storage
            foreach (var doc in app.Documents)
            {
                var blobClient = _blobServiceClient.GetBlobContainerClient("applications")
                    .GetBlobClient(doc.BlobUrl);
                await blobClient.DeleteIfExistsAsync();
            }
            
            _context.Applications.Remove(app);
        }
        
        // Purge draft applications (30 days inactive)
        var draftCutoff = DateTime.UtcNow.AddDays(-30);
        var oldDrafts = await _context.Applications
            .Where(a => a.Status == ApplicationStatus.Draft)
            .Where(a => a.LastModifiedDate < draftCutoff)
            .ToListAsync();
        
        _context.Applications.RemoveRange(oldDrafts);
        
        await _context.SaveChangesAsync();
    }
}
```

### Implementation: User Account Anonymization

```csharp
// When purging inactive user accounts (3 years)
public class UserPurgeService
{
    public async Task AnonymizeInactiveUsers(DateTime cutoffDate)
    {
        var inactiveUsers = await _context.Users
            .Where(u => u.LastLoginDate < cutoffDate && u.IsActive)
            .ToListAsync();
        
        foreach (var user in inactiveUsers)
        {
            // Anonymize PII (keep ID for audit referential integrity)
            user.Email = $"anonymized-{user.Id}@deleted.local";
            user.FirstName = "Anonymized";
            user.LastName = "User";
            user.IsActive = false;
            // Keep UserId for audit log references
        }
        
        await _context.SaveChangesAsync();
    }
}
```

## Consequences

### Positive

1. **Compliance** — Meets Government Records Act (7-year retention) and privacy laws
2. **Cost Optimization** — Auto-purge prevents unbounded storage growth
3. **Automation** — No manual intervention required for routine purging
4. **Audit Trail** — Purge actions are logged in `AuditLog` (who purged what when)
5. **Privacy** — User PII is anonymized, not deleted (preserves audit integrity)

### Negative

1. **Complexity** — Multiple retention policies to manage and monitor
2. **Risk of Accidental Deletion** — Incorrect purge logic could delete active data
3. **Testing Overhead** — Must test purge jobs with production-like data volumes
4. **Blob Lifecycle Limitations** — Cannot use application-specific logic (e.g., "purge only if application is rejected")

### Mitigations

- **Soft Delete First** — Mark records as "pending purge" for 30 days before hard delete
- **Backup Before Purge** — Export data to cold storage (Azure Storage Archive) before deletion
- **Monitoring** — Azure Monitor alerts on purge job failures or large deletions
- **Testing** — Run purge jobs in staging environment with production data copy
- **Audit Logging** — All purge actions logged with user/system ID and justification

## Alternatives Considered

### Keep Everything Forever

- **ALT-001**: **Description**: Never delete any data; rely on Azure Storage scale
- **ALT-001**: **Rejection Reason**: Violates privacy laws (GDPR); unbounded storage costs; not compliant with government records management

### Manual Purging by Admin

- **ALT-002**: **Description**: Admin periodically reviews and deletes old records
- **ALT-002**: **Rejection Reason**: Error-prone; not scalable; no audit trail of who purged what; violates "automated compliance" principle

### Azure SQL Temporal Tables Only

- **ALT-003**: **Description**: Use temporal tables for point-in-time recovery, purge from history table
- **ALT-003**: **Rejection Reason**: Doesn't handle Blob Storage documents; complex query for purging; temporal tables have retention limits

## References

- **PRD F-20**: Audit log retention: 7 years (compliance requirement)
- **PRD NFR-09**: Audit logs immutable, 7-year retention
- **ADR-003**: Azure SQL + Blob Storage (data storage locations)
- **ADR-009**: Azure Key Vault (store purge job connection strings)
- **ADR-010**: Row-Level Security (ensure purge jobs respect RLS policies)
- **Government Records Act**: 7-year retention for permit records
- **GDPR/Privacy Laws**: Right to be forgotten (with government exemptions)

## Implementation Plan

### MVP (Milestone 9: Azure Deployment)

1. Configure Azure Blob Storage lifecycle management policy (drafts → delete after 30 days)
2. Create `DataPurgeFunction` (Azure Function, timer trigger daily at 2 AM)
3. Implement purge logic for draft applications (30 days)
4. Add audit logging for all purge actions
5. Document retention policies in `docs/engineering/data-retention-policy.md`

### Phase 2 (Post-MVP)

1. Implement 7-year purge for approved/rejected applications
2. Add user account anonymization (3 years inactivity)
3. Create monitoring dashboard for purge job status
4. Add "export before purge" feature for compliance reporting
5. Document GDPR compliance approach for citizen data

---

**Next Steps:**

1. Add data retention policy to `docs/PRDs/atlas-mvp-prd.md` Section 9 (Constraints)
2. Create `docs/engineering/data-retention-policy.md` with detailed procedures
3. Add purge job to `plans/atlas-foundation-plan.md` Milestone 9
4. Update Bicep templates (ADR-007) to include Blob lifecycle policy
