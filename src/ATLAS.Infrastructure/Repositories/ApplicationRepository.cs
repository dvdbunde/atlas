using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ATLAS.Infrastructure.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly ApplicationDbContext _context;

        public ApplicationRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Application?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Application>> GetByCitizenIdAsync(Guid citizenId, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .Where(a => a.CitizenId == citizenId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Application>> GetByStatusAsync(ApplicationStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .Where(a => a.Status == status)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Application>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteAsync(Application entity, CancellationToken cancellationToken = default)
        {
            _context.Applications.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task AddAsync(Application entity, CancellationToken cancellationToken = default)
        {
            await _context.Applications.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(Application entity, CancellationToken cancellationToken = default)
        {
            _context.Applications.Update(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Applications.AnyAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Application>> GetByOfficerIdAsync(Guid officerId, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .Where(a => a.Reviews.Any(r => r.OfficerId == officerId))
                .ToListAsync(cancellationToken);
        }
    }
}
