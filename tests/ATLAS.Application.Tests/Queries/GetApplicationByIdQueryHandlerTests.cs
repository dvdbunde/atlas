using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Applications;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class GetApplicationByIdQueryHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly GetApplicationByIdQueryHandler _handler;

        public GetApplicationByIdQueryHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockPermitTypeRepository = new Mock<IPermitTypeRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(s => s.Role).Returns("Admin");
            _handler = new GetApplicationByIdQueryHandler(
                _mockRepository.Object,
                _mockUserRepository.Object,
                _mockPermitTypeRepository.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidId_ShouldReturnApplicationDetailDto()
        {
            // Arrange
            var application = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit();

            _mockRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var query = new GetApplicationByIdQuery { ApplicationId = application.Id };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(application.Id, result.Id);
            Assert.False(string.IsNullOrEmpty(result.ApplicationNumber));
            Assert.Equal(ApplicationStatus.Submitted, result.Status);
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldReturnNull()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Application)null);

            var query = new GetApplicationByIdQuery { ApplicationId = applicationId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_CitizenRole_ShouldReturnNull_WhenNotOwnApplication()
        {
            // Arrange
            var citizenId = Guid.NewGuid();
            var otherCitizenId = Guid.NewGuid();
            var application = new Domain.Entities.Application(otherCitizenId, Guid.NewGuid(), "Someone else's app");
            application.Submit();

            _mockRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockCurrentUserService.Setup(s => s.Role).Returns("Citizen");
            _mockCurrentUserService.Setup(s => s.UserId).Returns(citizenId);

            var query = new GetApplicationByIdQuery { ApplicationId = application.Id };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_CitizenRole_ShouldReturnApplication_WhenOwnApplication()
        {
            // Arrange
            var citizenId = Guid.NewGuid();
            var application = new Domain.Entities.Application(citizenId, Guid.NewGuid(), "My app");
            application.Submit();

            _mockRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _mockCurrentUserService.Setup(s => s.Role).Returns("Citizen");
            _mockCurrentUserService.Setup(s => s.UserId).Returns(citizenId);

            var query = new GetApplicationByIdQuery { ApplicationId = application.Id };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GetApplicationByIdQueryHandler(
                    null,
                    new Mock<IUserRepository>().Object,
                    new Mock<IPermitTypeRepository>().Object,
                    new Mock<ICurrentUserService>().Object));
        }
    }
}
