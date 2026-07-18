using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Interfaces;
using ATLAS.Domain;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class ResubmitApplicationCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<ResubmitApplicationCommandHandler>> _mockLogger;
        private readonly ResubmitApplicationCommandHandler _handler;
        private readonly Guid _testUserId;
        private readonly Guid _permitTypeId;
        private readonly Guid _officerId;
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepository;        

        public ResubmitApplicationCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<ResubmitApplicationCommandHandler>>();
            _mockPermitTypeRepository = new Mock<IPermitTypeRepository>();
            _testUserId = Guid.NewGuid();
            _permitTypeId = Guid.NewGuid();
            _officerId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
            _handler = new ResubmitApplicationCommandHandler(
                _mockRepository.Object,
                _mockCurrentUserService.Object,
                _mockMediator.Object,
                _mockLogger.Object,
                _mockPermitTypeRepository.Object);

            // Default permit type mock for valid scenarios
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PermitType("Building Permit", "desc", 150m));
        }

        private ATLAS.Domain.Entities.Application CreateInfoRequestedApplication()
        {
            var app = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            app.Submit();
            app.StartReview(_officerId);
            app.AssignToOfficer(_officerId);
            app.RequestInfo(_officerId, "Please provide more info");
            
            return app;
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldResubmitApplication()
        {
            // Arrange
            var application = CreateInfoRequestedApplication();
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            var command = new ResubmitApplicationCommand { ApplicationId = Guid.NewGuid() };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(ApplicationStatus.UnderReview, application.Status);
            _mockRepository.Verify(r => r.UpdateAsync(application, It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationResubmittedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ATLAS.Domain.Entities.Application?)null);
            var command = new ResubmitApplicationCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NotOwnApplication_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(otherUserId, _permitTypeId, "Notes");
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            var command = new ResubmitApplicationCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_IncorrectStatus_ShouldThrowDomainException()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            application.Submit(); // Submitted, not InfoRequested
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            var command = new ResubmitApplicationCommand { ApplicationId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<DomainException>(
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
        public async Task Handle_ShouldNotPublishEvent_WhenApplicationNotFound()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ATLAS.Domain.Entities.Application?)null);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(new ResubmitApplicationCommand { ApplicationId = Guid.NewGuid() }, CancellationToken.None));

            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationResubmittedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldNotPublishEvent_WhenNotOwnApplication()
        {
            var otherUserId = Guid.NewGuid();
            var application = new ATLAS.Domain.Entities.Application(otherUserId, _permitTypeId, "Notes");
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(new ResubmitApplicationCommand { ApplicationId = Guid.NewGuid() }, CancellationToken.None));

            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationResubmittedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldNotPublishEvent_WhenIncorrectStatus()
        {
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            application.Submit(); // Submitted, not InfoRequested
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            await Assert.ThrowsAsync<DomainException>(
                () => _handler.Handle(new ResubmitApplicationCommand { ApplicationId = Guid.NewGuid() }, CancellationToken.None));

            _mockMediator.Verify(m => m.Publish(It.IsAny<ApplicationResubmittedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldPreserveAssignment()
        {
            var application = CreateInfoRequestedApplication();
            var assignedOfficerId = application.AssignedOfficerId;
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            await _handler.Handle(new ResubmitApplicationCommand { ApplicationId = Guid.NewGuid() }, CancellationToken.None);

            Assert.Equal(assignedOfficerId, application.AssignedOfficerId);
            Assert.Equal(ApplicationStatus.UnderReview, application.Status);
        }
    }
}