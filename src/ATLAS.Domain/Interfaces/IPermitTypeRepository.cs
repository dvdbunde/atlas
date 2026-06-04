using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Domain.Interfaces
{
    public interface IPermitTypeRepository : IRepository<Entities.PermitType>
    {
        // Inherited from IRepository<PermitType>:
        // Task<PermitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Task<IEnumerable<PermitType>> GetAllAsync(CancellationToken cancellationToken = default);
        // Task AddAsync(PermitType entity, CancellationToken cancellationToken = default);
        // Task UpdateAsync(PermitType entity, CancellationToken cancellationToken = default);
        // Task DeleteAsync(PermitType entity, CancellationToken cancellationToken = default);

        // Specialized methods:
        Task<IEnumerable<Entities.PermitType>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
