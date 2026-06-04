using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Domain.Interfaces;

namespace ATLAS.Domain.Interfaces
{
    public interface IAuditLogRepository : IRepository<Entities.AuditLog>
    {
        // Inherited from IRepository<AuditLog>:
        // Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Task<IEnumerable<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default);
        // Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default);
        // Note: UpdateAsync and DeleteAsync inherited but NOT used (AuditLog is immutable)

        // Specialized methods (AuditLog is immutable - no update/delete):
        Task<IEnumerable<Entities.AuditLog>> GetByUserIdAsync(Guid? userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Entities.AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Entities.AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
