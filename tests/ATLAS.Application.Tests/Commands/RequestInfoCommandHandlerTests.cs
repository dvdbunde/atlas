using ATLAS.Application.Interfaces;
using MediatR;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;
using ATLAS.Application.Commands.Applications;

namespace ATLAS.Application.Tests.Commands
{
    public class RequestInfoCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly RequestInfoCommandHandler _handler;
        private readonly Guid _testOfficerId;

        public RequestInfoCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockMediator = new Mock<IMediator>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _testOfficerId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testOfficerId);
            _handler = new RequestInfoCommandHandler(
                _mockRepository.Object,
                _mockMediator.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldRequestInfo()
        {
            var applicationId = Guid.NewGuid();
            var officerId = _testOfficerId;
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.StartReview(officerId);
            application.AssignToOfficer(officerId);

            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new RequestInfoCommand
            {
                ApplicationId = applicationId,
                Message = "Please provide additional information"
            };

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

            var command = new RequestInfoCommand
            {
                ApplicationId = applicationId,
                Message = "Please provide additional information"
            };

            var result = await _handler.Handle(command, CancellationToken.None);
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenNotUnderReview()
        {
            var applicationId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new RequestInfoCommand { ApplicationId = applicationId, Message = "need more" };

            await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenNotAssignedToOfficer()
        {
            var applicationId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.StartReview(_testOfficerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new RequestInfoCommand { ApplicationId = applicationId, Message = "need more" };

            await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenCurrentUserHasNoId()
        {
            _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);
            var applicationId = Guid.NewGuid();
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();
            application.StartReview(_testOfficerId);
            application.AssignToOfficer(_testOfficerId);
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var command = new RequestInfoCommand { ApplicationId = applicationId, Message = "need more" };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
        }
    }
}
