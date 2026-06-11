using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Behaviors;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using MediatR;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Behaviors
{
    public class UserSynchronizationBehaviorTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IIdentityResolver> _mockIdentityResolver;
        private readonly UserSynchronizationBehavior<DummyRequest, DummyResponse> _behavior;

        public UserSynchronizationBehaviorTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockIdentityResolver = new Mock<IIdentityResolver>();
            _behavior = new UserSynchronizationBehavior<DummyRequest, DummyResponse>(
                _mockCurrentUserService.Object,
                _mockIdentityResolver.Object);
        }

        [Fact]
        public async Task Handle_WhenUnauthenticated_ShouldSkipSync()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(false);
            var nextCalled = false;

            // Act
            await _behavior.Handle(
                new DummyRequest(),
                () =>
                {
                    nextCalled = true;
                    return Task.FromResult(new DummyResponse());
                },
                CancellationToken.None);

            // Assert
            _mockIdentityResolver.Verify(r => r.SynchronizeUserAsync(It.IsAny<CancellationToken>()), Times.Never);
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task Handle_WhenAuthenticated_ShouldSynchronizeUser()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            var nextCalled = false;

            // Act
            await _behavior.Handle(
                new DummyRequest(),
                () =>
                {
                    nextCalled = true;
                    return Task.FromResult(new DummyResponse());
                },
                CancellationToken.None);

            // Assert
            _mockIdentityResolver.Verify(r => r.SynchronizeUserAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task Handle_ShouldCallNext_WhenSyncSucceeds()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(true);
            _mockIdentityResolver
                .Setup(r => r.SynchronizeUserAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User("test@example.com", "Test", "User", UserRole.Citizen));

            // Act
            var result = await _behavior.Handle(
                new DummyRequest(),
                () => Task.FromResult(new DummyResponse()),
                CancellationToken.None);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenCurrentUserServiceIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new UserSynchronizationBehavior<DummyRequest, DummyResponse>(
                    null!,
                    _mockIdentityResolver.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenIdentityResolverIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new UserSynchronizationBehavior<DummyRequest, DummyResponse>(
                    _mockCurrentUserService.Object,
                    null!));
        }

        // Dummy request/response types for testing the generic behavior
        private class DummyRequest : IRequest<DummyResponse> { }
        private class DummyResponse { }
    }
}
