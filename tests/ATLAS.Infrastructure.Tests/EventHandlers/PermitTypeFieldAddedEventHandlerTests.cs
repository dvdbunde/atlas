using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;
using ATLAS.Domain.ValueObjects;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class PermitTypeFieldAddedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly PermitTypeFieldAddedEventHandler _handler;

        public PermitTypeFieldAddedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _handler = new PermitTypeFieldAddedEventHandler(_auditLogRepository);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var fieldName = "AdditionalComments";            
            var evt = new PermitTypeFieldAddedEvent(permitTypeId, fieldName, FieldType.Text);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("PermitType", permitTypeId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("PermitTypeFieldAdded", log.Action);
            Assert.Equal("PermitType", log.EntityType);
            Assert.Equal(permitTypeId, log.EntityId);
            Assert.Contains(fieldName, log.Details);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PermitTypeFieldAddedEventHandler(null!));
        }
    }
}
