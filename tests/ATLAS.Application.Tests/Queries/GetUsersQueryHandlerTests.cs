using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;
using ATLAS.Application.Queries.Users;

namespace ATLAS.Application.Tests.Queries
{
    public class GetUsersQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly GetUsersQueryHandler _handler;

        public GetUsersQueryHandlerTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            _handler = new GetUsersQueryHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_NoRoleFilter_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User(Guid.NewGuid(), "user1@test.com", "John", "Doe", UserRole.Citizen),
                new User(Guid.NewGuid(), "user2@test.com", "Jane", "Smith", UserRole.Officer)
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            var query = new GetUsersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task Handle_WithRoleFilter_ShouldReturnFilteredUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User(Guid.NewGuid(), "user1@test.com", "John", "Doe", UserRole.Citizen),
                new User(Guid.NewGuid(), "user2@test.com", "Jane", "Smith", UserRole.Officer),
                new User(Guid.NewGuid(), "user3@test.com", "Bob", "Johnson", UserRole.Citizen)
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            var query = new GetUsersQuery { Role = "Citizen" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, u => Assert.Equal("Citizen", u.Role));
        }

        [Fact]
        public async Task Handle_WithInvalidRole_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User(Guid.NewGuid(), "user1@test.com", "John", "Doe", UserRole.Citizen),
                new User(Guid.NewGuid(), "user2@test.com", "Jane", "Smith", UserRole.Officer)
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            var query = new GetUsersQuery { Role = "InvalidRole" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count()); // Should return all when role is invalid
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GetUsersQueryHandler(null));
        }
    }

    public class GetUserByIdQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly GetUserByIdQueryHandler _handler;

        public GetUserByIdQueryHandlerTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            _handler = new GetUserByIdQueryHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_ValidUserId_ShouldReturnUserDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User(userId, "test@test.com", "John", "Doe", UserRole.Citizen);
            
            _mockRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var query = new GetUserByIdQuery { UserId = userId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("test@test.com", result.Email);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("Doe", result.LastName);
            Assert.Equal("Citizen", result.Role);
            Assert.Null(result.LastLoginDate); // Newly created, never logged in
        }

        [Fact]
        public async Task Handle_InvalidUserId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            var query = new GetUserByIdQuery { UserId = userId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GetUserByIdQueryHandler(null));
        }
    }
}
