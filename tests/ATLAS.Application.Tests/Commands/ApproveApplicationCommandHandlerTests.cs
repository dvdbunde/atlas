using ATLAS.Application.Interfaces;
using MediatR;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;
using ATLAS.Application.Commands.Applications;

namespace ATLAS.Application.Tests.Commands
{
    public class ApproveApplicationCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly ApproveApplicationCommandHandler _handler;
        private readonly Guid _testOfficerId;

        public ApproveApplicationCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockMediator = new Mock<IMediator>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _testOfficerId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testOfficerId);
            _handler = new ApproveApplicationCommandHandler(
                _mockRepository.Object,
                _mockMediator.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldApproveApplication()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = _testOfficerId;
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.StartReview(officerId); // Move to UnderReview

            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new ApproveApplicationCommand
            {
                ApplicationId = applicationId,
                Comments = "Approved"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldReturnFalse()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Application)null);

            var command = new ApproveApplicationCommand
            {
                ApplicationId = applicationId,
                Comments = "Approved"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            // Assert
            Assert.False(result);
        }
    }
}
