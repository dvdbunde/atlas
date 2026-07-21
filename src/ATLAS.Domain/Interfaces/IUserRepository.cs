using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;

namespace ATLAS.Domain.Interfaces
{
    public interface IUserRepository : IRepository<Entities.User>
    {
        // Inherited from IRepository<User>:
        // Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
        // Task AddAsync(User entity, CancellationToken cancellationToken = default);
        // Task UpdateAsync(User entity, CancellationToken cancellationToken = default);
        //
        // DeleteAsync is intentionally hidden (new) and unsupported. The User aggregate is a
        // synchronized Entra projection (ADR-013); ATLAS never deletes local user projections.
        // Deletion of identity is owned exclusively by Microsoft Entra ID.
        new Task DeleteAsync(User entity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException(
                "User projections are synchronized from Entra ID and must never be deleted by ATLAS (ADR-013).");

        // Specialized methods:
        Task<IEnumerable<Entities.User>> GetByRoleAsync(Entities.UserRole role, CancellationToken cancellationToken = default);
        Task<Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
