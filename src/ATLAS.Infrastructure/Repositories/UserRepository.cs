using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ATLAS.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .ToListAsync(cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
        {
            _context.Users.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Users.AnyAsync(u => u.Id == id, cancellationToken);
        }
    }
}
