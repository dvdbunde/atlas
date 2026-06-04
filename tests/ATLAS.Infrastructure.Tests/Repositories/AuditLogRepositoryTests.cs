using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Repositories
{
    public class AuditLogRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _repository;

        public AuditLogRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _repository = new AuditLogRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldAddAuditLogToDatabase()
        {
            // Arrange
            var auditLog = new AuditLog(Guid.NewGuid(), "SubmitApplication", "Application", Guid.NewGuid(), 
                "Application submitted", "127.0.0.1");
            
            await _repository.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            // Assert
            var saved = await _context.AuditLogs.FirstOrDefaultAsync(al => al.Id == auditLog.Id);
            Assert.NotNull(saved);
            Assert.Equal("SubmitApplication", saved.Action);
        }

        [Fact]
        public async Task GetByUserIdAsync_WithUserLogs_ShouldReturnUserLogs()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var log1 = new AuditLog(userId, "Action1", "Application", Guid.NewGuid(), "Details1", "127.0.0.1");
            var log2 = new AuditLog(userId, "Action2", "Application", Guid.NewGuid(), "Details2", "127.0.0.1");
            var otherLog = new AuditLog(Guid.NewGuid(), "Other", "Application", Guid.NewGuid(), "Other", "127.0.0.1");
            
            _context.AuditLogs.AddRange(log1, log2, otherLog);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUserIdAsync(userId);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByEntityAsync_WithEntityLogs_ShouldReturnEntityLogs()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var log1 = new AuditLog(Guid.NewGuid(), "Action1", "Application", entityId, "Details1", "127.0.0.1");
            var log2 = new AuditLog(Guid.NewGuid(), "Action2", "Application", entityId, "Details2", "127.0.0.1");
            
            _context.AuditLogs.AddRange(log1, log2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByEntityAsync("Application", entityId);

            // Assert
            Assert.Equal(2, result.Count());
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
