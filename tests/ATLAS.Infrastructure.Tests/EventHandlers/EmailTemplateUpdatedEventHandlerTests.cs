using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain;
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
            _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
            _currentUserMock.Setup(c => c.UserId).Returns(userId);
            var evt = new EmailTemplateUpdatedEvent("ApprovalNotification");

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
        public async Task Handle_WhenUserNotAuthenticated_ShouldThrowDomainException()
        {
            _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
            _currentUserMock.Setup(c => c.UserId).Returns((Guid?)null);
            var evt = new EmailTemplateUpdatedEvent("RejectionNotification");

            await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(evt, CancellationToken.None));
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

