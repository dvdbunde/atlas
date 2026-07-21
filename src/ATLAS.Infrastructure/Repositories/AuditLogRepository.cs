using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ATLAS.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditLogRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogs
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogs
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid? userId, CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogs
                .Where(a => a.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogs
                .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default)
        {
            await _context.AuditLogs.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(AuditLog entity, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Updating an AuditLog is not supported");
        }

        public Task DeleteAsync(AuditLog entity, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Deleting an AuditLog is not supported");
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.AuditLogs.AnyAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<PagedAuditLogResult> GetPagedAsync(
            AuditLogFilter filter,
            AuditLogSortOption sort,
            AuditLogPage page,
            CancellationToken cancellationToken = default)
        {
            filter ??= new AuditLogFilter();
            page ??= new AuditLogPage();

            // Build the query against the database (IQueryable) so filtering, sorting,
            // and paging happen server-side. The DbSet is IQueryable; nothing is
            // materialized until the final projection below.
            var query = _context.AuditLogs.AsNoTracking();

            if (filter.UserId.HasValue)
                query = query.Where(a => a.UserId == filter.UserId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(a => a.Action == filter.Action);

            if (filter.DateFrom.HasValue)
                query = query.Where(a => a.Timestamp >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(a => a.Timestamp <= filter.DateTo.Value);

            if (filter.EntityId.HasValue)
                query = query.Where(a => a.EntityId == filter.EntityId.Value);

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                query = query.Where(a => a.EntityType == filter.EntityType);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(a =>
                    a.Action.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    a.EntityType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    a.Details.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            query = sort == AuditLogSortOption.TimestampAsc
                ? query.OrderBy(a => a.Timestamp)
                : query.OrderByDescending(a => a.Timestamp);

            var totalCount = await query.CountAsync(cancellationToken);

            var pageNumber = page.PageNumber < 1 ? 1 : page.PageNumber;
            var pageSize = page.PageSize < 1 ? 20 : page.PageSize;

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedAuditLogResult
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
