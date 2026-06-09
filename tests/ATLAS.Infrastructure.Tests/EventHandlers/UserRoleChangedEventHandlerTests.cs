using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class UserRoleChangedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly UserRoleChangedEventHandler _handler;

        public UserRoleChangedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _handler = new UserRoleChangedEventHandler(_auditLogRepository);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldRole = ATLAS.Domain.Entities.UserRole.Citizen;
            var newRole = ATLAS.Domain.Entities.UserRole.Officer;
            var evt = new UserRoleChangedEvent(userId, oldRole, newRole);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("User", userId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("UserRoleChanged", log.Action);
            Assert.Equal("User", log.EntityType);
            Assert.Equal(userId, log.EntityId);
            Assert.Contains(oldRole.ToString(), log.Details);
            Assert.Contains(newRole.ToString(), log.Details);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UserRoleChangedEventHandler(null!));
        }
    }
}
