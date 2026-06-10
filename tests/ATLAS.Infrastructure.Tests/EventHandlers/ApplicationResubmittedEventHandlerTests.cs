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
    public class ApplicationResubmittedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly ApplicationResubmittedEventHandler _handler;

        public ApplicationResubmittedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _handler = new ApplicationResubmittedEventHandler(_auditLogRepository);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var citizenId = Guid.NewGuid();
            var evt = new ApplicationResubmittedEvent(applicationId, citizenId);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", applicationId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("ApplicationResubmitted", log.Action);
            Assert.Equal("Application", log.EntityType);
            Assert.Equal(applicationId, log.EntityId);
            Assert.Contains(citizenId.ToString(), log.Details);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationResubmittedEventHandler(null!));
        }
    }
}
