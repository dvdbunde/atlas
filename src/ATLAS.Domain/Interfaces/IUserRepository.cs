using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Domain.Interfaces
{
    public interface IUserRepository : IRepository<Entities.User>
    {
        // Inherited from IRepository<User>:
        // Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        // Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
        // Task AddAsync(User entity, CancellationToken cancellationToken = default);
        // Task UpdateAsync(User entity, CancellationToken cancellationToken = default);
        // Task DeleteAsync(User entity, CancellationToken cancellationToken = default);

        // Specialized methods:
        Task<IEnumerable<Entities.User>> GetByRoleAsync(Entities.UserRole role, CancellationToken cancellationToken = default);
        Task<Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
