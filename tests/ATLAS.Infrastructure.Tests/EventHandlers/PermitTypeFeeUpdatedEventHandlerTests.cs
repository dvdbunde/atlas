using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.EventHandlers;
using Moq;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class PermitTypeFeeUpdatedEventHandlerTests
    {
        private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
        private readonly PermitTypeFeeUpdatedEventHandler _handler;
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        public PermitTypeFeeUpdatedEventHandlerTests()
        {
            _handler = new PermitTypeFeeUpdatedEventHandler(_auditLogRepository.Object, _currentUserService.Object);
        }

        [Fact]
        public async Task Handle_ShouldPersistAuditLog()
        {
            var evt = new PermitTypeFeeUpdatedEvent(Guid.NewGuid(), 100m, 250m);

            await _handler.Handle(evt, CancellationToken.None);

            _auditLogRepository.Verify(r => r.AddAsync(
                It.Is<AuditLog>(l => l.Action == "PermitTypeFeeUpdated" && l.EntityId == evt.PermitTypeId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_NullRepository_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new PermitTypeFeeUpdatedEventHandler(null!, _currentUserService.Object));
        }
    }
}