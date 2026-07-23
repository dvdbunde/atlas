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
    public class PermitTypeDocumentRequirementUpdatedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly PermitTypeDocumentRequirementUpdatedEventHandler _handler;
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        public PermitTypeDocumentRequirementUpdatedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _handler = new PermitTypeDocumentRequirementUpdatedEventHandler(_auditLogRepository, _currentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var requirementId = Guid.NewGuid();
            var documentType = "Passport";
            var evt = new PermitTypeDocumentRequirementUpdatedEvent(permitTypeId, requirementId, documentType, true);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("DocumentRequirement", requirementId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("Updated", log.Action);
            Assert.Equal("DocumentRequirement", log.EntityType);
            Assert.Equal(requirementId, log.EntityId);
            Assert.Contains(documentType, log.Details);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PermitTypeDocumentRequirementUpdatedEventHandler(null!, _currentUserService.Object));
        }
    }
}
