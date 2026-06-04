using System;
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
    public class ConcurrencyTests : IDisposable
    {
        private readonly ApplicationDbContext _context1;
        private readonly ApplicationDbContext _context2;
        private readonly ApplicationRepository _repository1;
        private readonly ApplicationRepository _repository2;

        public ConcurrencyTests()
        {
            var options1 = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ConcurrencyTest")
                .Options;
            var options2 = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ConcurrencyTest")
                .Options;
            
            _context1 = new ApplicationDbContext(options1);
            _context2 = new ApplicationDbContext(options2);
            _repository1 = new ApplicationRepository(_context1);
            _repository2 = new ApplicationRepository(_context2);
        }

        [Fact]
        public async Task UpdateAsync_ConcurrentUpdates_ShouldNotCrash()
        {
            // Arrange: Create an application
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            _context1.Applications.Add(application);
            await _context1.SaveChangesAsync();

            // Act: Simulate concurrent updates (both repos load and update the same entity)
            var app1 = await _repository1.GetByIdAsync(application.Id);
            var app2 = await _repository2.GetByIdAsync(application.Id);

            // Both add a document and update
            app1.AddDocument(Guid.NewGuid(), "doc1.pdf", "application/pdf", 1024, "http://blob1.url", Guid.NewGuid());
            app2.AddDocument(Guid.NewGuid(), "doc2.pdf", "application/pdf", 2048, "http://blob2.url", Guid.NewGuid());

            // These should not crash (InMemory provider behavior may vary)
            await _repository1.UpdateAsync(app1);
            await _repository2.UpdateAsync(app2);

            // Assert: At least one document should be there (last write wins in InMemory)
            var updatedApp = await _repository1.GetByIdAsync(application.Id);
            Assert.True(updatedApp.Documents.Count >= 1); // At least one update persisted
        }

        [Fact]
        public async Task GetByIdAsync_ConcurrentReads_ShouldNotInterfere()
        {
            // Arrange: Create an application
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            _context1.Applications.Add(application);
            await _context1.SaveChangesAsync();

            // Act: Simulate concurrent reads
            var task1 = _repository1.GetByIdAsync(application.Id);
            var task2 = _repository2.GetByIdAsync(application.Id);

            var results = await Task.WhenAll(task1, task2);

            // Assert: Both should return the same application
            Assert.NotNull(results[0]);
            Assert.NotNull(results[1]);
            Assert.Equal(results[0].Id, results[1].Id);
        }

        public void Dispose()
        {
            _context1?.Dispose();
            _context2?.Dispose();
        }
    }
}
