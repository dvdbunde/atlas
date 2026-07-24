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
    public class PermitTypeGeneralInformationUpdatedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly PermitTypeGeneralInformationUpdatedEventHandler _handler;
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        public PermitTypeGeneralInformationUpdatedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _handler = new PermitTypeGeneralInformationUpdatedEventHandler(_auditLogRepository, _currentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var evt = new PermitTypeGeneralInformationUpdatedEvent(permitTypeId, "New Name", "New description");

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("PermitType", permitTypeId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("PermitTypeGeneralInformationUpdated", log.Action);
            Assert.Equal("PermitType", log.EntityType);
            Assert.Equal(permitTypeId, log.EntityId);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ShouldThrowDomainException()
        {
            // Arrange
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(false);
            _currentUserService.Setup(x => x.UserId).Returns((Guid?)null);
            var evt = new PermitTypeGeneralInformationUpdatedEvent(Guid.NewGuid(), "New Name", "New description");

            // Act & Assert
            await Assert.ThrowsAsync<ATLAS.Domain.DomainException>(() => _handler.Handle(evt, CancellationToken.None));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PermitTypeGeneralInformationUpdatedEventHandler(null!, _currentUserService.Object));
        }
    }
}
