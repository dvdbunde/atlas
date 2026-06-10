using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class DeactivatePermitTypeCommandHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockRepository;
        private readonly DeactivatePermitTypeCommandHandler _handler;

        public DeactivatePermitTypeCommandHandlerTests()
        {
            _mockRepository = new Mock<IPermitTypeRepository>();
            _handler = new DeactivatePermitTypeCommandHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldDeactivatePermitTypeAndReturnTrue()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var permitType = new PermitType("Test Type", "Description", 100.00m);

            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new DeactivatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                DeactivatedByAdminId = adminId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.False(permitType.IsActive);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyInactive_ShouldReturnTrueAndRemainInactive()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var permitType = new PermitType("Test Type", "Description", 100.00m);
            permitType.Deactivate(adminId); // Start as inactive
            Assert.False(permitType.IsActive);

            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new DeactivatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                DeactivatedByAdminId = adminId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.False(permitType.IsActive);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidPermitTypeId_ShouldReturnFalse()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PermitType)null);

            var command = new DeactivatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                DeactivatedByAdminId = Guid.NewGuid()
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DeactivatePermitTypeCommandHandler(null!));
        }
    }
}