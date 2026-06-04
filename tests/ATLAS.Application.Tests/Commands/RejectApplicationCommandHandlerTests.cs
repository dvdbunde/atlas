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
        private readonly RejectApplicationCommandHandler _handler;

        public RejectApplicationCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockMediator = new Mock<IMediator>();
            _handler = new RejectApplicationCommandHandler(_mockRepository.Object, _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldRejectApplication()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.StartReview(officerId); // Move to UnderReview
            
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new RejectApplicationCommand
            {
                ApplicationId = applicationId,
                OfficerId = officerId,
                ReasonCode = "INCOMPLETE",
                Comments = "Rejected"
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

            var command = new RejectApplicationCommand
            {
                ApplicationId = applicationId,
                OfficerId = Guid.NewGuid(),
                ReasonCode = "INCOMPLETE",
                Comments = "Rejected"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
        }
    }
}
