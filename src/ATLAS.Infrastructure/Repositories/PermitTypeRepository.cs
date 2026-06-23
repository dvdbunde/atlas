using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ATLAS.Infrastructure.Repositories
{
    public class PermitTypeRepository : IPermitTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public PermitTypeRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<PermitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.PermitTypes
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<PermitType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.PermitTypes
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<PermitType>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _context.PermitTypes
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(PermitType entity, CancellationToken cancellationToken = default)
        {
            await _context.PermitTypes.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(PermitType entity, CancellationToken cancellationToken = default)
        {
            _context.PermitTypes.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(PermitType entity, CancellationToken cancellationToken = default)
        {
            _context.PermitTypes.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.PermitTypes.AnyAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<string?> GetNameByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var permitType = await _context.PermitTypes
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken);
            
            return permitType;
        }
    }
}
