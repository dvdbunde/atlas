using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class UpdatePermitTypeGeneralInformationCommandHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockRepository = new();
        private readonly UpdatePermitTypeGeneralInformationCommandHandler _handler;

        public UpdatePermitTypeGeneralInformationCommandHandlerTests()
        {
            _handler = new UpdatePermitTypeGeneralInformationCommandHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldUpdateNameAndDescription()
        {
            var permitTypeId = Guid.NewGuid();
            var permitType = new PermitType("Original Name", "Original description", 100.00m);

            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitType);

            var command = new UpdatePermitTypeGeneralInformationCommand
            {
                PermitTypeId = permitTypeId,
                Name = "Updated Name",
                Description = "Updated description"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.True(result);
            Assert.Equal("Updated Name", permitType.Name);
            Assert.Equal("Updated description", permitType.Description);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NotFound_ShouldReturnFalse()
        {
            var permitTypeId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PermitType)null);

            var command = new UpdatePermitTypeGeneralInformationCommand
            {
                PermitTypeId = permitTypeId,
                Name = "Updated Name",
                Description = "Updated description"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(result);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
