using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;
using ATLAS.Domain.ValueObjects;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using ATLAS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class PermitTypeFieldAddedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly PermitTypeFieldAddedEventHandler _handler;
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        public PermitTypeFieldAddedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _handler = new PermitTypeFieldAddedEventHandler(_auditLogRepository, _currentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var fieldId = Guid.NewGuid();
            var fieldName = "AdditionalComments";
            var evt = new PermitTypeFieldAddedEvent(permitTypeId, fieldId, fieldName, FieldType.Text);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("PermitField", evt.FieldId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("Added", log.Action);
            Assert.Equal("PermitField", log.EntityType);
            Assert.Equal(evt.FieldId, log.EntityId);
            Assert.Contains(fieldName, log.Details);
              Assert.Equal(_currentUserService.Object.UserId, log.UserId);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PermitTypeFieldAddedEventHandler(null!, _currentUserService.Object));
        }
    }
}
