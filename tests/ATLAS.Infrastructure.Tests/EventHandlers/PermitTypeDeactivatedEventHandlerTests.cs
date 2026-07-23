using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Events;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using ATLAS.Infrastructure.Repositories;
using ATLAS.Application.Interfaces;
using Moq;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class PermitTypeDeactivatedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly Mock<ICurrentUserService> _currentUserService;
        private readonly PermitTypeDeactivatedEventHandler _handler;

        public PermitTypeDeactivatedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _currentUserService = new Mock<ICurrentUserService>();
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _handler = new PermitTypeDeactivatedEventHandler(_auditLogRepository, _currentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var adminId = Guid.NewGuid();            
            var evt = new PermitTypeDeactivatedEvent(permitTypeId);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("PermitType", permitTypeId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("PermitTypeDeactivated", log.Action);
            Assert.Equal("PermitType", log.EntityType);
            Assert.Equal(permitTypeId, log.EntityId);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);
            Assert.Contains(_currentUserService.Object.UserId.ToString(), log.Details);            
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PermitTypeDeactivatedEventHandler(null!, _currentUserService.Object));
        }
    }
}
