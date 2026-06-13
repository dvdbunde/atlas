using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.Services;
using Moq;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Services
{
    public class IdentityResolverTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IdentityResolver _resolver;

        public IdentityResolverTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _resolver = new IdentityResolver(
                _mockCurrentUserService.Object,
                _mockUserRepository.Object,
                _mockUnitOfWork.Object);
        }

        #region SynchronizeUserAsync

        [Fact]
        public async Task SynchronizeUserAsync_WithExistingUser_ShouldUpdateLastLoginDate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(userId, "existing@test.com", "Existing", "User", UserRole.Citizen);

            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockCurrentUserService.Setup(s => s.Email).Returns("existing@test.com");
            _mockCurrentUserService.Setup(s => s.Claims).Returns(new List<Claim>());
            _mockCurrentUserService.Setup(s => s.Role).Returns("Citizen");

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _resolver.SynchronizeUserAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result.LastLoginDate);
            _mockUserRepository.Verify(r => r.UpdateAsync(existingUser, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SynchronizeUserAsync_WithNewIdentity_ShouldCreateUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockCurrentUserService.Setup(s => s.Email).Returns("new@test.com");
            _mockCurrentUserService.Setup(s => s.Role).Returns("Citizen");
            _mockCurrentUserService.Setup(s => s.Claims).Returns(new List<Claim>());

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockUserRepository.Setup(r => r.GetByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _resolver.SynchronizeUserAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new@test.com", result.Email);
            Assert.Equal("Unknown", result.FirstName);
            Assert.Equal("User", result.LastName);
            Assert.Equal(UserRole.Citizen, result.Role);
            _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SynchronizeUserAsync_WithChangedEmail_ShouldUpdateEmail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(userId, "old@test.com", "Existing", "User", UserRole.Officer);

            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockCurrentUserService.Setup(s => s.Email).Returns("newemail@test.com");
            _mockCurrentUserService.Setup(s => s.Role).Returns("Officer");
            _mockCurrentUserService.Setup(s => s.Claims).Returns(new List<Claim>());

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _resolver.SynchronizeUserAsync(CancellationToken.None);

            // Assert
            Assert.Equal("newemail@test.com", result.Email);
            _mockUserRepository.Verify(r => r.UpdateAsync(existingUser, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SynchronizeUserAsync_WithChangedNameFromClaims_ShouldUpdateProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(userId, "test@test.com", "OldFirst", "OldLast", UserRole.Citizen);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.GivenName, "NewFirst"),
                new Claim(ClaimTypes.Surname, "NewLast")
            };

            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockCurrentUserService.Setup(s => s.Email).Returns("test@test.com");
            _mockCurrentUserService.Setup(s => s.Role).Returns("Citizen");
            _mockCurrentUserService.Setup(s => s.Claims).Returns(claims);

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _resolver.SynchronizeUserAsync(CancellationToken.None);

            // Assert
            Assert.Equal("NewFirst", result.FirstName);
            Assert.Equal("NewLast", result.LastName);
        }

        [Fact]
        public async Task SynchronizeUserAsync_WithUnauthenticated_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.SynchronizeUserAsync(CancellationToken.None));
        }

        #endregion

        #region ResolveCurrentUserAsync

        [Fact]
        public async Task ResolveCurrentUserAsync_WithExistingUserById_ShouldReturnExisting()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(userId, "test@test.com", "Test", "User", UserRole.Citizen);

            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockCurrentUserService.Setup(s => s.Claims).Returns(new List<Claim>());

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _resolver.ResolveCurrentUserAsync(CancellationToken.None);

            // Assert
            Assert.Equal(existingUser.Id, result.Id);
        }

        [Fact]
        public async Task ResolveCurrentUserAsync_WithNonExistentUser_ShouldCreateNew()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockCurrentUserService.Setup(s => s.Email).Returns("newuser@test.com");
            _mockCurrentUserService.Setup(s => s.Role).Returns("Citizen");
            _mockCurrentUserService.Setup(s => s.Claims).Returns(new List<Claim>());

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockUserRepository.Setup(r => r.GetByEmailAsync("newuser@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _resolver.ResolveCurrentUserAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("newuser@test.com", result.Email);
            Assert.Equal("Unknown", result.FirstName);
            Assert.Equal("User", result.LastName);
            Assert.Equal(UserRole.Citizen, result.Role);
        }

        [Fact]
        public async Task ResolveCurrentUserAsync_WithUnauthenticated_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _resolver.ResolveCurrentUserAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ResolveCurrentUserAsync_WhenNotFoundById_ShouldFallbackToEmail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(userId, "email@test.com", "Email", "User", UserRole.Officer);

            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
            _mockCurrentUserService.Setup(s => s.Email).Returns("email@test.com");
            _mockCurrentUserService.Setup(s => s.Claims).Returns(new List<Claim>());

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockUserRepository.Setup(r => r.GetByEmailAsync("email@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _resolver.ResolveCurrentUserAsync(CancellationToken.None);

            // Assert
            Assert.Equal(existingUser.Id, result.Id);
            Assert.Equal("email@test.com", result.Email);
        }

        #endregion

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenCurrentUserServiceIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new IdentityResolver(null!, _mockUserRepository.Object, _mockUnitOfWork.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenUserRepositoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new IdentityResolver(_mockCurrentUserService.Object, null!, _mockUnitOfWork.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenUnitOfWorkIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new IdentityResolver(_mockCurrentUserService.Object, _mockUserRepository.Object, null!));
        }
    }
}
