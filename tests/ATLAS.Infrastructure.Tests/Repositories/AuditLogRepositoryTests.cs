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
            var exception = await Assert.ThrowsAsync<NotSupportedException>(() => 
                _repository.UpdateAsync(auditLog));           
            

            // Assert - AuditLog is immutable, should not be allowed to update
            Assert.Contains("Updating an AuditLog is not supported", exception.Message);                                 
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveAuditLogFromDatabase()
        {
            // Arrange
            var auditLog = new AuditLog(Guid.NewGuid(), "Delete", "Entity", Guid.NewGuid(), "Delete", "127.0.0.1");
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Act
              var exception = await Assert.ThrowsAsync<NotSupportedException>(() => 
                _repository.DeleteAsync(auditLog));                                  

            // Assert - AuditLog is immutable, should not be allowed to delete
            Assert.Contains("Deleting an AuditLog is not supported", exception.Message);                                 
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


        [Fact]
        public async Task GetPagedAsync_WithNoFilter_ShouldReturnAllLogsAndTotalCount()
        {
            var logs = new[]
            {
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Application", Guid.NewGuid(), "Details 2", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Delete", "Permit", Guid.NewGuid(), "Details 3", "127.0.0.1")
            };
            _context.AuditLogs.AddRange(logs);
            await _context.SaveChangesAsync();

            var result = await _repository.GetPagedAsync(new AuditLogFilter(), AuditLogSortOption.TimestampDesc, new AuditLogPage { PageNumber = 1, PageSize = 20 });

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task GetPagedAsync_WithPageSize_ShouldReturnSinglePage()
        {
            var logs = Enumerable.Range(0, 5).Select(i =>
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), $"Details {i}", "127.0.0.1")).ToArray();
            _context.AuditLogs.AddRange(logs);
            await _context.SaveChangesAsync();

            var result = await _repository.GetPagedAsync(new AuditLogFilter(), AuditLogSortOption.TimestampDesc, new AuditLogPage { PageNumber = 2, PageSize = 2 });

            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.PageNumber);
            Assert.Equal(3, result.TotalPages);
        }

        [Fact]
        public async Task GetPagedAsync_WithActionFilter_ShouldReturnMatchingLogs()
        {
            _context.AuditLogs.AddRange(
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Application", Guid.NewGuid(), "Details 2", "127.0.0.1"));
            await _context.SaveChangesAsync();

            var result = await _repository.GetPagedAsync(
                new AuditLogFilter { Action = "Create" }, AuditLogSortOption.TimestampDesc, new AuditLogPage());

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Create", result.Items[0].Action);
        }

        [Fact]
        public async Task GetPagedAsync_WithSearchTerm_ShouldMatchActionEntityTypeOrDetails()
        {
            _context.AuditLogs.AddRange(
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "alpha details", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Permit", Guid.NewGuid(), "other details", "127.0.0.1"));
            await _context.SaveChangesAsync();

            var result = await _repository.GetPagedAsync(
                new AuditLogFilter { SearchTerm = "PERMIT" }, AuditLogSortOption.TimestampDesc, new AuditLogPage());

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Permit", result.Items[0].EntityType);
        }

        [Fact]
        public async Task GetPagedAsync_WithSortAscending_ShouldOrderOldestFirst()
        {
            var older = new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "older", "127.0.0.1");
            var newer = new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "newer", "127.0.0.1");
            _context.AuditLogs.AddRange(older, newer);
            await _context.SaveChangesAsync();

            var result = await _repository.GetPagedAsync(
                new AuditLogFilter(), AuditLogSortOption.TimestampAsc, new AuditLogPage());

            Assert.Equal(older.Id, result.Items[0].Id);
            Assert.Equal(newer.Id, result.Items[1].Id);
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyResult_ShouldReturnZeroTotal()
        {
            var result = await _repository.GetPagedAsync(
                new AuditLogFilter { Action = "Nonexistent" }, AuditLogSortOption.TimestampDesc, new AuditLogPage());

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetPagedAsync_WithPartialActionFilter_ShouldReturnSubstringMatches()
        {
            _context.AuditLogs.AddRange(
                new AuditLog(Guid.NewGuid(), "ApplicationSubmitted", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "PermitActivated", "Permit", Guid.NewGuid(), "Details 2", "127.0.0.1"));
            await _context.SaveChangesAsync();

            var result = await _repository.GetPagedAsync(
                new AuditLogFilter { Action = "App" }, AuditLogSortOption.TimestampDesc, new AuditLogPage());

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("ApplicationSubmitted", result.Items[0].Action);
        }

        [Fact]
        public async Task GetPagedAsync_WithPartialEntityTypeFilter_ShouldReturnSubstringMatches()
        {
            _context.AuditLogs.AddRange(
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "Details 1", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Create", "Permit", Guid.NewGuid(), "Details 2", "127.0.0.1"));
            await _context.SaveChangesAsync();

            var result = await _repository.GetPagedAsync(
                new AuditLogFilter { EntityType = "per" }, AuditLogSortOption.TimestampDesc, new AuditLogPage());

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Permit", result.Items[0].EntityType);
        }

        [Fact]
        public async Task GetPagedAsync_WithSearchTerm_ShouldBeCaseInsensitive()
        {
            _context.AuditLogs.AddRange(
                new AuditLog(Guid.NewGuid(), "Create", "Application", Guid.NewGuid(), "alpha details", "127.0.0.1"),
                new AuditLog(Guid.NewGuid(), "Update", "Permit", Guid.NewGuid(), "other details", "127.0.0.1"));
            await _context.SaveChangesAsync();

            var result = await _repository.GetPagedAsync(
                new AuditLogFilter { SearchTerm = "permit" }, AuditLogSortOption.TimestampDesc, new AuditLogPage());

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Permit", result.Items[0].EntityType);
        }        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

