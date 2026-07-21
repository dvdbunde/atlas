using System;
using System.Collections.Generic;

namespace ATLAS.Domain.Interfaces
{
    /// <summary>
    /// Server-side filter options for audit log queries.
    /// Free-text search is intentionally limited to Action / EntityType / Details
    /// (case-insensitive contains). UserId, EntityId, and Timestamp are represented
    /// through dedicated structured filters, not free text.
    /// </summary>
    public class AuditLogFilter
    {
        public Guid? UserId { get; set; }
        public string? Action { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public Guid? EntityId { get; set; }
        public string? EntityType { get; set; }
        public string? SearchTerm { get; set; }
    }

    public enum AuditLogSortOption
    {
        TimestampDesc,
        TimestampAsc
    }

    /// <summary>
    /// Paging request for audit log queries. Audit logs are append-only and can grow
    /// indefinitely, so paging is always performed database-side.
    /// </summary>
    public class AuditLogPage
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Paged result returned by the repository. Items are materialized; counts and
    /// page metadata allow the caller to build UI pagination without re-querying.
    /// </summary>
    public class PagedAuditLogResult
    {
        public IReadOnlyList<Entities.AuditLog> Items { get; init; } = Array.Empty<Entities.AuditLog>();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
