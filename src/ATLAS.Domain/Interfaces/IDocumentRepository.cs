using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Domain.Interfaces
{
    public interface IDocumentRepository : IRepository<Entities.Document>
    {
        // Inherited from IRepository<Document>:
        // Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default);
        // Task AddAsync(Document entity, CancellationToken cancellationToken = default);
        // Task UpdateAsync(Document entity, CancellationToken cancellationToken = default);
        // Task DeleteAsync(Document entity, CancellationToken cancellationToken = default);

        // Specialized methods:
        Task<IEnumerable<Entities.Document>> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
