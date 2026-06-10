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
    public class PermitTypeDeactivatedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly PermitTypeDeactivatedEventHandler _handler;

        public PermitTypeDeactivatedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _handler = new PermitTypeDeactivatedEventHandler(_auditLogRepository);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var adminId = Guid.NewGuid();            
            var evt = new PermitTypeDeactivatedEvent(permitTypeId, adminId);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("PermitType", permitTypeId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("PermitTypeDeactivated", log.Action);
            Assert.Equal("PermitType", log.EntityType);
            Assert.Equal(permitTypeId, log.EntityId);
            Assert.Contains(adminId.ToString(), log.Details);            
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PermitTypeDeactivatedEventHandler(null!));
        }
    }
}
