using Entities = ATLAS.Domain.Entities;
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

        public async Task<Entities.Application?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Entities.Application>> GetByCitizenIdAsync(Guid citizenId, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .AsNoTracking()
                .Where(a => a.CitizenId == citizenId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Entities.Application>> GetByStatusAsync(ApplicationStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .AsNoTracking()
                .Where(a => a.Status == status)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Entities.Application>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteAsync(Entities.Application entity, CancellationToken cancellationToken = default)
        {
            _context.Applications.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task AddAsync(Entities.Application entity, CancellationToken cancellationToken = default)
        {
            await _context.Applications.AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(Entities.Application entity, CancellationToken cancellationToken = default)
        {
            _context.Applications.Update(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Applications.AnyAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Entities.Application>> GetByOfficerIdAsync(Guid officerId, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .AsNoTracking()
                .Where(a => a.Reviews.Any(r => r.OfficerId == officerId))
                .ToListAsync(cancellationToken);
        }

        // Document access methods (Documents are owned by Application aggregate)
        public async Task<Entities.Document?> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .SelectMany(a => a.Documents)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        }

        public async Task<IEnumerable<Entities.Document>> GetDocumentsByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
        {
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
            
            return application?.Documents ?? Enumerable.Empty<Entities.Document>();
        }

        public Task<bool> DocumentExistsAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return _context.Applications
                .SelectMany(a => a.Documents)
                .AsNoTracking()
                .AnyAsync(d => d.Id == documentId, cancellationToken);
        }

        // Review access methods (Reviews are owned by Application aggregate)
        public async Task<Entities.Review?> GetReviewByIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .SelectMany(a => a.Reviews)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        }

        public async Task<IEnumerable<Entities.Review>> GetReviewsByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
        {
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
            
            return application?.Reviews ?? Enumerable.Empty<Entities.Review>();
        }

        // Phase H+B - PermitType access methods
        public async Task<Entities.PermitType?> GetPermitTypeByIdAsync(Guid permitTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.PermitTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == permitTypeId, cancellationToken);
        }

        public async Task<IEnumerable<Entities.PermitType>> GetActivePermitTypesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.PermitTypes
                .AsNoTracking()
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);
        }

        // Phase H+B - FieldValues access (FieldValues are owned by Application aggregate)
        public async Task<IEnumerable<Entities.ApplicationFieldValue>> GetFieldValuesByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
        {
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
            
            return application?.FieldValues ?? Enumerable.Empty<Entities.ApplicationFieldValue>();
        }
    }
}
