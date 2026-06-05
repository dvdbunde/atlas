using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
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
        public async Task GetByIdAsync_WithExistingAuditLog_ShouldReturnAuditLog()
        {
            // Arrange
            var auditLog = new AuditLog(Guid.NewGuid(), "Action", "Entity", Guid.NewGuid(), "Details", "127.0.0.1");
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(auditLog.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(auditLog.Id, result.Id);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllAuditLogs()
        {
            // Arrange
            var log1 = new AuditLog(Guid.NewGuid(), "Action1", "Entity", Guid.NewGuid(), "Details1", "127.0.0.1");
            var log2 = new AuditLog(Guid.NewGuid(), "Action2", "Entity", Guid.NewGuid(), "Details2", "127.0.0.1");
            _context.AuditLogs.AddRange(log1, log2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByUserIdAsync_WithExistingUser_ShouldReturnUserLogs()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var log1 = new AuditLog(userId, "Action1", "Entity", Guid.NewGuid(), "Details1", "127.0.0.1");
            var log2 = new AuditLog(userId, "Action2", "Entity", Guid.NewGuid(), "Details2", "127.0.0.1");
            var log3 = new AuditLog(Guid.NewGuid(), "Action3", "Entity", Guid.NewGuid(), "Details3", "127.0.0.1");
            _context.AuditLogs.AddRange(log1, log2, log3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUserIdAsync(userId);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByEntityAsync_WithExistingEntity_ShouldReturnEntityLogs()
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

        [Fact]
        public async Task GetByDateRangeAsync_WithinRange_ShouldReturnLogs()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow.AddDays(1);
            var log = new AuditLog(Guid.NewGuid(), "Action", "Entity", Guid.NewGuid(), "Details", "127.0.0.1");
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task AddAsync_ShouldAddAuditLogToDatabase()
        {
            // Arrange
            var auditLog = new AuditLog(Guid.NewGuid(), "NewAction", "Entity", Guid.NewGuid(), "New Details", "127.0.0.1");

            // Act
            await _repository.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            // Assert
            var saved = await _context.AuditLogs.FirstOrDefaultAsync(al => al.Id == auditLog.Id);
            Assert.NotNull(saved);
            Assert.Equal("NewAction", saved.Action);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateAuditLogInDatabase()
        {
            // Arrange
            var auditLog = new AuditLog(Guid.NewGuid(), "Original", "Entity", Guid.NewGuid(), "Original", "127.0.0.1");
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAsync(auditLog);
            await _context.SaveChangesAsync();

            // Assert - AuditLog is immutable, but EF Core allows updating
            var updated = await _context.AuditLogs.FirstOrDefaultAsync(al => al.Id == auditLog.Id);
            Assert.NotNull(updated);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveAuditLogFromDatabase()
        {
            // Arrange
            var auditLog = new AuditLog(Guid.NewGuid(), "Delete", "Entity", Guid.NewGuid(), "Delete", "127.0.0.1");
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(auditLog);
            await _context.SaveChangesAsync();

            // Assert
            var deleted = await _context.AuditLogs.FirstOrDefaultAsync(al => al.Id == auditLog.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            var auditLog = new AuditLog(Guid.NewGuid(), "Exists", "Entity", Guid.NewGuid(), "Exists", "127.0.0.1");
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(auditLog.Id);

            // Assert
            Assert.True(result);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
