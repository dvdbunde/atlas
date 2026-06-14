using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Domain.Interfaces
{
    public interface IApplicationRepository : IRepository<Entities.Application>
    {
        // Inherited from IRepository<Application>:
        // Task<Application?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Task<IEnumerable<Application>> GetAllAsync(CancellationToken cancellationToken = default);
        // Task AddAsync(Application entity, CancellationToken cancellationToken = default);
        // Task UpdateAsync(Application entity, CancellationToken cancellationToken = default);
        // Task DeleteAsync(Application entity, CancellationToken cancellationToken = default);

        // Specialized methods:
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Entities.Application>> GetByCitizenIdAsync(Guid citizenId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Entities.Application>> GetByStatusAsync(Enums.ApplicationStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<Entities.Application>> GetByOfficerIdAsync(Guid officerId, CancellationToken cancellationToken = default);

        // Document access methods (Documents are owned by Application aggregate)
        Task<Entities.Document?> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Entities.Document>> GetDocumentsByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
        Task<bool> DocumentExistsAsync(Guid documentId, CancellationToken cancellationToken = default);

        // Review access methods (Reviews are owned by Application aggregate)
        Task<Entities.Review?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Entities.Review>> GetReviewsByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);

        // Phase H+B - Milestone 5: Additional methods for draft workflow
        Task<Entities.PermitType?> GetPermitTypeByIdAsync(Guid permitTypeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Entities.PermitType>> GetActivePermitTypesAsync(CancellationToken cancellationToken = default);
    }
}
