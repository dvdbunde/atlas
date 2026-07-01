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
    }
}
