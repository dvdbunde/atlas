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
    public class ReleaseApplicationCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMediator> _mockMediator;
        private readonly ReleaseApplicationCommandHandler _handler;

        public ReleaseApplicationCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMediator = new Mock<IMediator>();
            _handler = new ReleaseApplicationCommandHandler(_mockRepository.Object, _mockCurrentUser.Object, _mockMediator.Object);
        }

        private static Entities.Application CreateUnderReviewAssignedTo(Guid officerId)
        {
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.AssignToOfficer(officerId);
            return application;
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldReleaseAndReturnTrue()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = CreateUnderReviewAssignedTo(officerId);

            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new ReleaseApplicationCommand { ApplicationId = applicationId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Equal(ApplicationStatus.Submitted, application.Status);
            Assert.Null(application.AssignedOfficerId);
            Assert.Null(application.AssignedDate);
            _mockRepository.Verify(r => r.UpdateAsync(application, It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationReleasedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldRefreshModifiedDate()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = CreateUnderReviewAssignedTo(officerId);
            var originalModifiedDate = application.ModifiedDate;

            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new ReleaseApplicationCommand { ApplicationId = applicationId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(originalModifiedDate);
            Assert.True(application.ModifiedDate > originalModifiedDate,
                "ModifiedDate should advance on a successful release");
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldReturnFalse()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Application?)null);

            var command = new ReleaseApplicationCommand { ApplicationId = applicationId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_MissingUserId_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            _mockCurrentUser.Setup(u => u.UserId).Returns((Guid?)null);

            var command = new ReleaseApplicationCommand { ApplicationId = applicationId };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_AssignedToOtherOfficer_ShouldThrowDomainException()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var currentOfficerId = Guid.NewGuid();
            var otherOfficerId = Guid.NewGuid();
            var application = CreateUnderReviewAssignedTo(otherOfficerId);

            _mockCurrentUser.Setup(u => u.UserId).Returns(currentOfficerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new ReleaseApplicationCommand { ApplicationId = applicationId };

            // Act & Assert
            await Assert.ThrowsAsync<ATLAS.Domain.DomainException>(
                () => _handler.Handle(command, CancellationToken.None));
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NotUnderReview_ShouldThrowDomainException()
        {
            // Arrange — Submitted, not Under Review
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();

            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new ReleaseApplicationCommand { ApplicationId = applicationId };

            // Act & Assert
            await Assert.ThrowsAsync<ATLAS.Domain.DomainException>(
                () => _handler.Handle(command, CancellationToken.None));
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Unassigned_ShouldThrowDomainException()
        {
            // Arrange — Under Review but unassigned
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.StartReview(officerId);

            _mockCurrentUser.Setup(u => u.UserId).Returns(officerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new ReleaseApplicationCommand { ApplicationId = applicationId };

            // Act & Assert
            await Assert.ThrowsAsync<ATLAS.Domain.DomainException>(
                () => _handler.Handle(command, CancellationToken.None));
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
