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
    public class UpdatePermitTypeCommandHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockRepository;
        private readonly Mock<IMediator> _mockMediator; 
        private readonly UpdatePermitTypeCommandHandler _handler;

        public UpdatePermitTypeCommandHandlerTests()
        {
            _mockRepository = new Mock<IPermitTypeRepository>();
            _mockMediator = new Mock<IMediator>();
            _handler = new UpdatePermitTypeCommandHandler(_mockRepository.Object, _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_DeactivateFlag_ShouldNotChangeState()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();
            var permitType = new PermitType("Test Type", "Description", 100.00m);
            Assert.True(permitType.IsActive); // Starts as active

            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new UpdatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                IsActive = false
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            // Update command does NOT deactivate — use DeactivatePermitTypeCommand for that
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

            var command = new UpdatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                IsActive = false
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_WithFee_ShouldUpdateFeeAndReturnTrue()
        {
            var permitTypeId = Guid.NewGuid();
            var permitType = new PermitType("Test Type", "Description", 100.00m);

            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new UpdatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                Fee = 199.99m
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result);
            Assert.Equal(199.99m, permitType.Fee);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Once);
        }

    }
}
