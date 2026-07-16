using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using ATLAS.Application.Interfaces;
using Moq;
using Xunit;
using ATLAS.Application.Commands.Applications;

namespace ATLAS.Application.Tests.Commands
{
    public class AssignApplicationToMeCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMediator> _mockMediator;
        private readonly AssignApplicationToMeCommandHandler _handler;

        public AssignApplicationToMeCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMediator = new Mock<IMediator>();
            _handler = new AssignApplicationToMeCommandHandler(_mockRepository.Object, _mockCurrentUser.Object, _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldAssignToCurrentOfficerAndReturnTrue()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();

            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new AssignApplicationToMeCommand { ApplicationId = applicationId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Equal(officerId, application.AssignedOfficerId);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationAssignedToOfficerEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyAssignedToMe_ShouldNotPublishEvent()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.AssignToOfficer(officerId);

            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new AssignApplicationToMeCommand { ApplicationId = applicationId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationAssignedToOfficerEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AlreadyAssignedToOtherOfficer_ShouldThrowDomainException()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var otherOfficerId = Guid.NewGuid();
            var currentOfficerId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.AssignToOfficer(otherOfficerId);

            _mockCurrentUser.Setup(u => u.UserId).Returns(currentOfficerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new AssignApplicationToMeCommand { ApplicationId = applicationId };

            // Act & Assert
            await Assert.ThrowsAsync<ATLAS.Domain.DomainException>(
                () => _handler.Handle(command, CancellationToken.None));
            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationAssignedToOfficerEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_MissingUserId_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            _mockCurrentUser.Setup(u => u.UserId).Returns((Guid?)null);

            var command = new AssignApplicationToMeCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldReturnFalse()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            _mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Application)null);

            var command = new AssignApplicationToMeCommand { ApplicationId = applicationId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_NullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_InvalidStatus_ShouldPropagateDomainException()
        {
            // Arrange: Draft app is not assignable.
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            // stays Draft

            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new AssignApplicationToMeCommand { ApplicationId = applicationId };

            // Act & Assert: aggregate throws, handler does not swallow it.
            await Assert.ThrowsAsync<ATLAS.Domain.DomainException>(
                () => _handler.Handle(command, CancellationToken.None));
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationAssignedToOfficerEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldNotCallSaveChangesDirectly()
        {
            // TransactionBehavior (pipeline) owns the commit; handler must not call SaveChanges.
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();

            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new AssignApplicationToMeCommand { ApplicationId = applicationId };

            await _handler.Handle(command, CancellationToken.None);

            // Handler calls UpdateAsync (tracked), NOT SaveChangesAsync.
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
