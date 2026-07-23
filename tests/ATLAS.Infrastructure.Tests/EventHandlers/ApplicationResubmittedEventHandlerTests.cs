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
    public class ApplicationResubmittedEventHandlerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogRepository _auditLogRepository;
        private readonly Mock<ICurrentUserService> _currentUserService;
        private readonly ApplicationResubmittedEventHandler _handler;

        public ApplicationResubmittedEventHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _auditLogRepository = new AuditLogRepository(_context);
            _currentUserService = new Mock<ICurrentUserService>();
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _handler = new ApplicationResubmittedEventHandler(_auditLogRepository, _currentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidEvent_ShouldPersistToAuditLog()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var citizenId = Guid.NewGuid();
            var evt = new ApplicationResubmittedEvent(applicationId);

            // Act
            await _handler.Handle(evt, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var auditLogs = await _auditLogRepository.GetByEntityAsync("Application", applicationId);
            var log = Assert.Single(auditLogs);
            Assert.Equal("ApplicationResubmitted", log.Action);
            Assert.Equal("Application", log.EntityType);
            Assert.Equal(applicationId, log.EntityId);
            Assert.Equal(_currentUserService.Object.UserId, log.UserId);
            Assert.Contains(_currentUserService.Object.UserId.ToString(), log.Details);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApplicationResubmittedEventHandler(null!, _currentUserService.Object));
        }
    }
}
