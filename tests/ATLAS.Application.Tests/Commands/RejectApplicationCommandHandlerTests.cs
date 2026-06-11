using ATLAS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class RejectApplicationCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly RejectApplicationCommandHandler _handler;
        private readonly Guid _testOfficerId;

        public RejectApplicationCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockMediator = new Mock<IMediator>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _testOfficerId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testOfficerId);
            _handler = new RejectApplicationCommandHandler(
                _mockRepository.Object,
                _mockMediator.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldRejectApplication()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = _testOfficerId;
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.StartReview(officerId);

            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new RejectApplicationCommand
            {
                ApplicationId = applicationId,
                ReasonCode = "INCOMPLETE",
                Comments = "Rejected"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            Assert.True(result);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldReturnFalse()
        {
            var applicationId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Application)null);

            var command = new RejectApplicationCommand
            {
                ApplicationId = applicationId,
                ReasonCode = "INCOMPLETE",
                Comments = "Rejected"
            };

            var result = await _handler.Handle(command, CancellationToken.None);
            Assert.False(result);
        }
    }
}
