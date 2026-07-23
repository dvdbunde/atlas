using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;
using ATLAS.Application.Commands.PermitTypes;

namespace ATLAS.Application.Tests.Commands
{
    public class ActivatePermitTypeCommandHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockRepository;
        private readonly ActivatePermitTypeCommandHandler _handler;
        private readonly Mock<IMediator> _mockMediator;

        public ActivatePermitTypeCommandHandlerTests()
        {
            _mockRepository = new Mock<IPermitTypeRepository>();
            _mockMediator = new Mock<IMediator>();
            _handler = new ActivatePermitTypeCommandHandler(_mockRepository.Object, _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldActivatePermitTypeAndReturnTrue()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var permitType = new PermitType("Test Type", "Description", 100.00m);
            permitType.Deactivate(adminId); // start inactive
            Assert.False(permitType.IsActive);

            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new ActivatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                ActivatedByAdminId = adminId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.True(permitType.IsActive);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyActive_ShouldReturnTrueAndRemainActive()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var permitType = new PermitType("Test Type", "Description", 100.00m); // active by default
            Assert.True(permitType.IsActive);

            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new ActivatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                ActivatedByAdminId = adminId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.True(permitType.IsActive);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidPermitTypeId_ShouldReturnFalse()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PermitType)null);

            var command = new ActivatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                ActivatedByAdminId = Guid.NewGuid()
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
            Assert.Throws<ArgumentNullException>(() => new ActivatePermitTypeCommandHandler(null!, _mockMediator.Object));
        }
    }
}