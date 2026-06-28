using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class TransactionBoundaryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ApplicationRepository _repository;

        public TransactionBoundaryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _repository = new ApplicationRepository(_context);
        }

        [Fact]
        public async Task GetDocumentByIdAsync_WhenDocumentExists_ShouldReturnDocument()
        {
            // Arrange
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            var documentId = Guid.NewGuid();
            application.AddDocument(documentId, "test.pdf", "application/pdf", 1024, "http://blob.url", Guid.NewGuid());
            
            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            // Act
            var document = await _repository.GetDocumentByIdAsync(documentId);

            // Assert
            Assert.NotNull(document);
            Assert.Equal("test.pdf", document.FileName);
        }

        [Fact]
        public async Task GetDocumentsByApplicationIdAsync_WhenApplicationHasDocuments_ShouldReturnDocuments()
        {
            // Arrange
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            var documentId = Guid.NewGuid();
            application.AddDocument(documentId, "test.pdf", "application/pdf", 1024, "http://blob.url", Guid.NewGuid());
            
            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            // Act
            var documents = await _repository.GetDocumentsByApplicationIdAsync(application.Id);

            // Assert
            Assert.Single(documents);
            Assert.Equal("test.pdf", documents.First().FileName);
        }

        [Fact]
        public async Task GetReviewByIdAsync_WhenReviewExists_ShouldReturnReview()
        {
            // Arrange
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");            
            var review = application.AddReview(Guid.NewGuid(), Guid.NewGuid(), ReviewDecision.Approve, "Approved comment", true, null);
            
            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            // Act
            var retrievedReview = await _repository.GetReviewByIdAsync(review.Id);

            // Assert
            Assert.NotNull(retrievedReview);
            Assert.Equal(ReviewDecision.Approve, retrievedReview.Decision);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
