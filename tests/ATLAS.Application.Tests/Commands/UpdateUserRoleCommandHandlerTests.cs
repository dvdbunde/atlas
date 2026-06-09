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
    public class UpdateUserRoleCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly UpdateUserRoleCommandHandler _handler;

        public UpdateUserRoleCommandHandlerTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            _handler = new UpdateUserRoleCommandHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldUpdateRoleAndReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User("test@example.com", "John", "Doe", UserRole.Citizen);

            _mockRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var command = new UpdateUserRoleCommand
            {
                UserId = userId,
                Role = "Officer"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Equal(UserRole.Officer, user.Role);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            var command = new UpdateUserRoleCommand
            {
                UserId = userId,
                Role = "Officer"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UpdateUserRoleCommandHandler(null!));
        }

    }
}
