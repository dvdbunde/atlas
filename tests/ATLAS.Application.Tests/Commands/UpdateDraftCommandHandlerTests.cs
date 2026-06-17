using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Interfaces;
using ATLAS.Domain;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class UpdateDraftCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<UpdateDraftCommandHandler>> _mockLogger;
        private readonly UpdateDraftCommandHandler _handler;
        private readonly Guid _testUserId;
        private readonly Guid _applicationId;
        private readonly Guid _permitTypeId;
        private readonly ATLAS.Domain.Entities.Application _existingApplication;

        public UpdateDraftCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<UpdateDraftCommandHandler>>();
            _testUserId = Guid.NewGuid();
            _permitTypeId = Guid.NewGuid();
            _applicationId = Guid.NewGuid();
            _existingApplication = new  ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Original notes");
            // Use reflection to set the Id for testing
            var idField = typeof(Entity<Guid>).GetProperty("Id",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            // Alternatively construct with known ID - we use a workaround
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
            _handler = new UpdateDraftCommandHandler(
                _mockRepository.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object);
        }

        private void SetupApplicationFound(ATLAS.Domain.Entities.Application application)
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldUpdateFieldValues()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Original notes");
            application.AddFieldValue("FieldA", "OldValue", 0);
            SetupApplicationFound(application);

            var command = new UpdateDraftCommand
            {
                ApplicationId = _applicationId,
                FieldValues = new Dictionary<string, string>
                {
                    { "FieldA", "NewValue" },       // update existing
                    { "FieldB", "AddedValue" }       // add new
                }
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("NewValue", application.FieldValues[0].Value);
            Assert.Equal(2, application.FieldValues.Count);
            _mockRepository.Verify(r => r.UpdateAsync(application, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUpdateCitizenNotes()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Original notes");
            SetupApplicationFound(application);

            var command = new UpdateDraftCommand
            {
                ApplicationId = _applicationId,
                CitizenNotes = "Updated notes",
                FieldValues = new Dictionary<string, string>()
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("Updated notes", application.CitizenNotes);
            _mockRepository.Verify(r => r.UpdateAsync(application, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ATLAS.Domain.Entities.Application?)null);
            var command = new UpdateDraftCommand { ApplicationId = Guid.NewGuid() };

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
            SetupApplicationFound(application);

            var command = new UpdateDraftCommand { ApplicationId = _applicationId };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NotDraftStatus_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var application = new ATLAS.Domain.Entities.Application(_testUserId, _permitTypeId, "Notes");
            application.Submit(); // Now Submitted
            SetupApplicationFound(application);

            var command = new UpdateDraftCommand { ApplicationId = _applicationId };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));
        }
    }
}