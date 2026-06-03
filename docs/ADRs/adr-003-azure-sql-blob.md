# ADR 003: Data Storage Strategy - Azure SQL + Azure Blob Storage

## Status

### Accepted

## Context

ATLAS needs to store two distinct types of data:

### 1. Structured/Relational Data

- **Applications** - Permit applications with status, dates, notes
- **Users** - Citizen, Officer, Admin user accounts
- **PermitTypes** - Configurable permit categories with fields and requirements
- **Reviews** - Officer reviews and decisions
- **AuditLogs** - Immutable audit trail (7-year retention)

### 2. Unstructured Data (Documents)

- **Uploaded Documents** - PDF, JPG, PNG files up to 25MB per file (PRD F-03)
- **Multiple documents per application** - Citizens upload supporting evidence

**Requirements:**

- **ACID transactions** for relational data (application submission, status changes)
- **Unlimited scale** for document storage (unlimited growth per PRD)
- **Cost-effective** - Serverless options for variable workloads
- **Compliance** - Data sovereignty (West Europe), encryption at rest
- **Performance** - Fast queries for dashboards, quick document retrieval

**Alternative storage options considered:**

| Option | Pros | Cons |
| -------- | ------ | ------ |
| **Azure SQL Only** (store docs as VARBINARY) | Single storage, ACID transactions | 25MB files × 1000s = huge DB, backup/restore slow |
| **Azure SQL + File System** | Simple document storage | No geo-redundancy, no CDN, server dependency |
| **Cosmos DB Only** (NoSQL) | Unlimited scale, multi-model | Overkill for relational data, higher cost, no SQL queries |
| **Azure SQL + Blob Storage** (SELECTED) | Best of both worlds | More complex (two storage systems) |

## Decision

We will use a **hybrid storage strategy**:

1. **Azure SQL Database (Serverless)** for structured/relational data
2. **Azure Blob Storage** for unstructured document storage

### Data Distribution

```text
┌──────────────────────────────────────────────────────────┐
│                    Azure SQL Database                    │
│  ┌──────────────┐  ┌─────────────┐  ┌────────────┐       │
│  │ Applications │  │  Users      │  │ PermitTypes│       │
│  └──────────────┘  └─────────────┘  └────────────┘       │
│  ┌──────────────┐  ┌─────────────┐  ┌────────────┐       │
│  │ Reviews      │  │ AuditLogs   │  │ Documents  │       │
│  │              │  │ (metadata)  │  │ (metadata) │       │
│  └──────────────┘  └─────────────┘  └────────────┘       │
└──────────────────────────────────────────────────────────┘
                           │
                           │ BlobUrl (reference)
                           ▼
┌──────────────────────────────────────────────────────────┐
│                  Azure Blob Storage                      │
│  ┌─────────────────────────────────────────────────┐     │
│  │ Container: "permit-documents"                   │     │
│  │  ┌─────────────────┐  ┌─────────────────┐       │     │
│  │  │ app-123/doc1.pdf│  │ app-456/doc2.jpg│       │     │
│  │  └─────────────────┘  └─────────────────┘       │     │
│  └─────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────┘
```

### Azure SQL Database Configuration

| Property | Value | Rationale |
| ---------- | ------- | ------------ |
| **Tier** | Serverless (General Purpose) | Auto-pauses during low activity, cost-effective for government workloads |
| **Hardware** | Gen5 (2-4 vCores) | Sufficient for MVP (500 concurrent users) |
| **Region** | West Europe | Data sovereignty compliance |
| **Backup** | 7-day retention (default) | Meets PRD requirement for daily backups |
| **Geo-redundancy** | Zone-redundant | High availability (99.9% uptime SLA) |

**EF Core Configuration:**

```csharp
// Atlas.Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public DbSet<Application> Applications { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<PermitType> PermitTypes { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Document> Documents { get; set; } // Metadata only

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Application>().ToTable("Applications");
        modelBuilder.Entity<Document>().ToTable("Documents");
        // Configure relationships, constraints, indexes...
    }
}
```

### Azure Blob Storage Configuration

| Property | Value | Rationale |
| ---------- | ------- | ------------ |
| **Account Kind** | StorageV2 (General Purpose v2) | Supports blobs, queues, files |
| **Performance** | Standard (HDD) | Cost-effective for documents, CDN optional |
| **Replication** | RA-GRS (Read-access geo-redundant) | Disaster recovery (1-hour RPO per PRD) |
| **Access Tier** | Hot (default) | Frequent access for active applications |
| **Container** | `permit-documents` | Organized by application ID |

**Blob Storage Service:**

```csharp
// Atlas.Infrastructure/Services/BlobStorageService.cs
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public async Task<string> UploadAsync(Guid applicationId, string fileName, Stream content)
    {
        var container = _blobServiceClient.GetBlobContainerClient("permit-documents");
        var blobPath = $"{applicationId}/{fileName}";
        var blobClient = container.GetBlobClient(blobPath);
        
        await blobClient.UploadAsync(content, overwrite: false);
        
        // Generate SAS token for secure access (optional)
        var sasUri = GenerateSasUri(blobClient);
        return sasUri.ToString();
    }
}
```

### Document Entity (Metadata in SQL)

```csharp
// Atlas.Domain/Entities/Document.cs
public class Document : Entity<Guid>
{
    public Guid ApplicationId { get; private set; }  // FK to Application
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long FileSize { get; private set; }
    public string BlobUrl { get; private set; }      // Reference to Blob Storage
    public DateTime UploadedDate { get; private set; }
    public Guid UploadedById { get; private set; }
}
```

## Consequences

### Positive

1. **Best Tool for the Job** - SQL for relational data, Blob for unstructured files
2. **Cost-Effective** - Serverless SQL auto-pauses, Blob Storage pay-per-use
3. **Scalability** - Blob Storage handles unlimited document growth, SQL scales vCores
4. **Performance** - SQL optimized for queries, Blob optimized for file streaming
5. **Compliance** - West Europe region, encryption at rest, 7-year retention possible
6. **Security** - SQL TDE (Transparent Data Encryption), Blob Storage Service Encryption
7. **CDN Ready** - Blob Storage can be fronted by Azure CDN for global access

### Negative

1. **Complexity** - Two storage systems to manage, backup, and monitor
2. **Eventual Consistency** - Document metadata in SQL, actual file in Blob (two-phase commit?)
3. **Backup/Restore** - Must coordinate SQL backups with Blob Storage backups
4. **Cross-system Queries** - Cannot JOIN between SQL and Blob Storage

### Mitigations

| Challenge | Mitigation |
| ----------- | ------------- |
| **Two storage systems** | Use Repository pattern to abstract storage details from Domain |
| **Eventual consistency** | Document upload is atomic (SQL metadata + Blob upload in same transaction scope) |
| **Backup coordination** | Use Azure Backup for SQL, Blob Storage point-in-time restore |
| **Cross-system queries** | Not needed - documents accessed by BlobUrl reference from SQL |

### Transaction Example

```csharp
// Application aggregate handles document upload atomically
public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, Guid>
{
    public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        // 1. Upload to Blob Storage (infrastructure)
        var blobUrl = await _blobStorage.UploadAsync(request.ApplicationId, request.FileName, request.Stream);

        // 2. Add document metadata to Application (domain + SQL)
        var application = await _repository.GetByIdAsync(request.ApplicationId);
        application.AddDocument(request.FileName, request.ContentType, blobUrl);
        
        await _repository.UpdateAsync(application); // Single SQL transaction
        
        return application.Id;
    }
}
```

## Compliance with Requirements

| Requirement | How Storage Strategy Addresses It |
| ------------- | -------------------------------------- |
| 25MB document uploads (F-03) | Blob Storage handles large files, SQL stores metadata only |
| 500 concurrent users | SQL Serverless auto-scales, Blob Storage unlimited |
| 99.9% uptime SLA | Azure SQL SLA 99.9%, Blob Storage SLA 99.9% |
| Daily backups, 30-day retention | SQL automatic backups, Blob Storage soft delete |
| Geo-redundant (1-hour RPO) | RA-GRS replication for both SQL and Blob |
| 7-year audit retention | SQL tables with retention policy, Blob Storage immutable storage |
| Data sovereignty (West Europe) | Both services deployed to West Europe region |
| Encryption at rest | SQL TDE, Blob Storage Service Encryption (AES-256) |

## References

- [ADR-001: Clean Architecture](adr-001-clean-architecture.md) (Infrastructure Layer)
- [ADR-002: CQRS with MediatR](adr-002-cqrs-mediatr.md)
- [ADR-006: GitHub Actions CI/CD](adr-006-github-actions.md) (EF Core Migrations in CI/CD)
- [ADR-007: Bicep Infrastructure as Code](adr-007-bicep.md) (SQL + Blob Bicep Modules)
- [ADR-008: Microsoft Entra ID Authentication](adr-008-microsoft-entra-id.md) (Entra ID Auth for SQL)
- [ATLAS PRD - Non-Functional Requirements](../PRDs/atlas-mvp-prd.md#6-non-functional-requirements)
- [Azure SQL Database Serverless](https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-overview)
- [Azure Blob Storage](https://learn.microsoft.com/azure/storage/blobs/)
- [EF Core with Azure SQL](https://learn.microsoft.com/ef/core/providers/sql-server/)
