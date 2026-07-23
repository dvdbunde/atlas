using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Events;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class PermitTypeFieldRemovedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly PermitTypeFieldRemovedEventHandler _handler;
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        public PermitTypeFieldRemovedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _handler = new PermitTypeFieldRemovedEventHandler(_auditLogRepository, _currentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var fieldId = Guid.NewGuid();
            var fieldName = "AdditionalComments";
            var evt = new PermitTypeFieldRemovedEvent(permitTypeId, fieldId, fieldName);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("PermitField", fieldId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("Removed", log.Action);
            Assert.Equal("PermitField", log.EntityType);
            Assert.Equal(fieldId, log.EntityId);
            Assert.Contains(fieldName, log.Details);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);    
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PermitTypeFieldRemovedEventHandler(null!, _currentUserService.Object));
        }
    }
}
