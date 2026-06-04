using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Entities = ATLAS.Domain.Entities;

namespace ATLAS.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IApplicationRepository _applications;
        private readonly IPermitTypeRepository _permitTypes;
        private readonly IUserRepository _users;
        private readonly IAuditLogRepository _auditLogs;

        public UnitOfWork(ApplicationDbContext context,
                               IApplicationRepository applications,
                               IPermitTypeRepository permitTypes,
                               IUserRepository users,
                               IAuditLogRepository auditLogs)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _applications = applications ?? throw new ArgumentNullException(nameof(applications));
            _permitTypes = permitTypes ?? throw new ArgumentNullException(nameof(permitTypes));
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _auditLogs = auditLogs ?? throw new ArgumentNullException(nameof(auditLogs));
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public IApplicationRepository Applications => _applications;
        public IPermitTypeRepository PermitTypes => _permitTypes;
        public IUserRepository Users => _users;
        public IAuditLogRepository AuditLogs => _auditLogs;
        
        // Documents and Reviews are accessed through ApplicationRepository (aggregate boundary)
        // No separate Document or Review repositories - they are owned by Application aggregate

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
