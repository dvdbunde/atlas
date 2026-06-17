using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class CreateDraftCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<CreateDraftCommandHandler>> _mockLogger;
        private readonly CreateDraftCommandHandler _handler;
        private readonly Guid _testUserId;

        public CreateDraftCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<CreateDraftCommandHandler>>();
            _testUserId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
            _handler = new CreateDraftCommandHandler(
                _mockRepository.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateDraftAndReturnId()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var command = new CreateDraftCommand
            {
                PermitTypeId = permitTypeId,
                CitizenNotes = "Test notes",
                FieldValues = new Dictionary<string, string>
                {
                    { "PropertyAddress", "123 Main St" },
                    { "LotSize", "5000" }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _mockRepository.Verify(r => r.AddAsync(
                It.Is<Domain.Entities.Application>(a =>
                    a.CitizenId == _testUserId &&
                    a.PermitTypeId == permitTypeId &&
                    a.CitizenNotes == "Test notes" &&
                    a.FieldValues.Count == 2 &&
                    a.Status == Domain.Enums.ApplicationStatus.Draft),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_UnauthenticatedUser_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);
            var command = new CreateDraftCommand { PermitTypeId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Application>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NoFieldValues_ShouldCreateDraftWithEmptyFieldValues()
        {
            // Arrange
            var command = new CreateDraftCommand
            {
                PermitTypeId = Guid.NewGuid(),
                FieldValues = new Dictionary<string, string>()
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _mockRepository.Verify(r => r.AddAsync(
                It.Is<Domain.Entities.Application>(a => a.FieldValues.Count == 0),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}