using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class CreatePermitTypeCommandHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockRepository;
        private readonly Mock<IMediator> _mockMediator;
        private readonly CreatePermitTypeCommandHandler _handler;

        public CreatePermitTypeCommandHandlerTests()
        {
            _mockRepository = new Mock<IPermitTypeRepository>();
            _mockMediator = new Mock<IMediator>();
            _handler = new CreatePermitTypeCommandHandler(_mockRepository.Object, _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreatePermitTypeAndReturnId()
        {
            // Arrange
            var command = new CreatePermitTypeCommand
            {
                Name = "Business Permit",
                Description = "Permit for business operations",
                Fee = 150.00m
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<PermitTypeActivatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreatePermitTypeWithCorrectProperties()
        {
            // Arrange
            var command = new CreatePermitTypeCommand
            {
                Name = "Building Permit",
                Description = "For construction projects",
                Fee = 250.00m
            };

            PermitType capturedPermitType = null;
            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<PermitType>(), It.IsAny<CancellationToken>()))
                .Callback<PermitType, CancellationToken>((pt, _) => capturedPermitType = pt)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedPermitType);
            Assert.Equal(command.Name, capturedPermitType.Name);
            Assert.Equal(command.Description, capturedPermitType.Description);
            Assert.Equal(command.Fee, capturedPermitType.Fee);
            Assert.True(capturedPermitType.IsActive);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CreatePermitTypeCommandHandler(null!, _mockMediator.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenMediatorIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CreatePermitTypeCommandHandler(_mockRepository.Object, null!));
        }
    }
}