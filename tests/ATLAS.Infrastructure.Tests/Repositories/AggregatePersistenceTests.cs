using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.ValueObjects;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class AggregatePersistenceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ApplicationRepository _applicationRepo;
        private readonly PermitTypeRepository _permitTypeRepo;
        private readonly UserRepository _userRepo;

        public AggregatePersistenceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _applicationRepo = new ApplicationRepository(_context);
            _permitTypeRepo = new PermitTypeRepository(_context);
            _userRepo = new UserRepository(_context);
        }

        [Fact]
        public async Task ApplicationAggregate_WithDocuments_ShouldPersistAndRetrieve()
        {
            // Arrange - Application IS the aggregate root with Documents and Reviews as owned entities
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            var doc1Id = Guid.NewGuid();
            var doc2Id = Guid.NewGuid();
            application.AddDocument(doc1Id, "doc1.pdf", "application/pdf", 1024, "http://blob1.url", Guid.NewGuid());
            application.AddDocument(doc2Id, "doc2.pdf", "application/pdf", 2048, "http://blob2.url", Guid.NewGuid());

            // Act - Save aggregate through ApplicationRepository
            await _applicationRepo.AddAsync(application);
            await _context.SaveChangesAsync();

            // Assert - Retrieve and verify owned entities persisted
            var retrieved = await _applicationRepo.GetByIdAsync(application.Id);
            Assert.NotNull(retrieved);
            Assert.Equal(2, retrieved.Documents.Count);
            Assert.Contains(retrieved.Documents, d => d.Id == doc1Id && d.FileName == "doc1.pdf");
            Assert.Contains(retrieved.Documents, d => d.Id == doc2Id && d.FileName == "doc2.pdf");
        }

        [Fact]
        public async Task ApplicationAggregate_WithReviews_ShouldPersistAndRetrieve()
        {
            // Arrange
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");            
            application.AddReview(Guid.NewGuid(), ReviewDecision.Approve, "Approved", true, null);

            // Act
            await _applicationRepo.AddAsync(application);
            await _context.SaveChangesAsync();

            // Assert
            var retrieved = await _applicationRepo.GetByIdAsync(application.Id);
            Assert.Single(retrieved.Reviews);
            Assert.Equal(ReviewDecision.Approve, retrieved.Reviews.First().Decision);
        }

        [Fact]
        public async Task PermitTypeAggregate_WithValueObjects_ShouldPersistAndRetrieve()
        {
            // Arrange - PermitType IS the aggregate root with PermitField and DocumentRequirement as value objects
            var permitType = new PermitType("Building Permit", "Description", 150.00m);
            permitType.AddField("SiteArea", FieldType.Number, true, "0");
            permitType.AddDocumentRequirement("SitePlan", true, new[] { ".pdf" }, 5 * 1024 * 1024);

            // Act
            await _permitTypeRepo.AddAsync(permitType);
            await _context.SaveChangesAsync();

            // Assert
            var retrieved = await _permitTypeRepo.GetByIdAsync(permitType.Id);
            Assert.NotNull(retrieved);
            Assert.Single(retrieved.Fields);
            Assert.Single(retrieved.DocumentRequirements);
            Assert.Equal("SiteArea", retrieved.Fields.First().Name);
            Assert.Equal("SitePlan", retrieved.DocumentRequirements.First().DocumentType);
        }

        [Fact]
        public async Task ApplicationAggregate_Delete_ShouldCascadeDeleteOwnedEntities()
        {
            // Arrange
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.AddDocument(Guid.NewGuid(), "doc.pdf", "application/pdf", 1024, "http://blob.url", Guid.NewGuid());
            application.AddReview(Guid.NewGuid(), ReviewDecision.Approve, "Approved", true, null);
            
            await _applicationRepo.AddAsync(application);
            await _context.SaveChangesAsync();

            // Act - Delete aggregate root (use original object to avoid tracking conflict)
            _context.Entry(application).State = EntityState.Detached; // Detach to avoid tracking conflict
            await _applicationRepo.DeleteAsync(application);
            await _context.SaveChangesAsync();

            // Assert - Owned entities should be cascade deleted
            var documents = await _applicationRepo.GetDocumentsByApplicationIdAsync(application.Id);
            var reviews = await _applicationRepo.GetReviewsByApplicationIdAsync(application.Id);
            Assert.Empty(documents);
            Assert.Empty(reviews);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
