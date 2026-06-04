using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ATLAS.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .SelectMany(a => a.Documents)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .SelectMany(a => a.Documents)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Document>> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
        {
            return await _context.Applications
                .Where(a => a.Id == applicationId)
                .SelectMany(a => a.Documents)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Document entity, CancellationToken cancellationToken = default)
        {
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == entity.ApplicationId, cancellationToken);
            
            if (application != null)
            {
                _context.Entry(application).State = EntityState.Modified;
            }
        }

        public Task UpdateAsync(Document entity, CancellationToken cancellationToken = default)
        {
            // Documents are owned entities, update through Application
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Document entity, CancellationToken cancellationToken = default)
        {
            // Documents are owned entities, removal should be done through Application
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Applications
                .SelectMany(a => a.Documents)
                .AnyAsync(d => d.Id == id, cancellationToken);
        }
    }
}
