using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Email;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Data;
using ATLAS.Infrastructure.EventHandlers;
using Moq;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class EmailTemplateUpdatedEventHandlerTests
    {
        private readonly Mock<IAuditLogRepository> _auditLogsMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly EmailTemplateUpdatedEventHandler _handler;

        public EmailTemplateUpdatedEventHandlerTests()
        {
            _auditLogsMock = new Mock<IAuditLogRepository>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _handler = new EmailTemplateUpdatedEventHandler(_auditLogsMock.Object, _currentUserMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldWriteAuditLogWithExpectedFields()
        {
            var userId = Guid.NewGuid();
            var evt = new EmailTemplateUpdatedEvent("ApprovalNotification", userId);

            await _handler.Handle(evt, CancellationToken.None);

            _auditLogsMock.Verify(r => r.AddAsync(
                It.Is<AuditLog>(l =>
                    l.UserId == userId &&
                    l.Action == "Updated" &&
                    l.EntityType == "EmailTemplate" &&
                    l.EntityId == evt.EntityId &&
                    l.Details.Contains("ApprovalNotification") &&
                    l.IpAddress == string.Empty),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenEventUserIdEmpty_FallsBackToCurrentUser()
        {
            var fallback = Guid.NewGuid();
            _currentUserMock.Setup(c => c.UserId).Returns(fallback);
            var evt = new EmailTemplateUpdatedEvent("RejectionNotification", Guid.Empty);

            await _handler.Handle(evt, CancellationToken.None);

            _auditLogsMock.Verify(r => r.AddAsync(
                It.Is<AuditLog>(l => l.UserId == fallback && l.EntityId == evt.EntityId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenAuditRepositoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new EmailTemplateUpdatedEventHandler(null!, Mock.Of<ICurrentUserService>()));
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenCurrentUserServiceIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new EmailTemplateUpdatedEventHandler(Mock.Of<IAuditLogRepository>(), null!));
        }
    }
}

