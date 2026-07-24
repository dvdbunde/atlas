using System;
using ATLAS.Application.Interfaces;
using ATLAS.Domain;
using ATLAS.Infrastructure.EventHandlers;
using Moq;
using Xunit;

namespace ATLAS.Infrastructure.Tests
{
    public class AuditGuardTests
    {
        private readonly Mock<ICurrentUserService> _currentUserService = new();

        [Fact]
        public void RequireAuthenticatedUser_WhenAuthenticated_ShouldReturnUserId()
        {
            var expected = Guid.NewGuid();
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns(expected);

            var result = AuditGuard.RequireAuthenticatedUser(_currentUserService.Object, "test action");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void RequireAuthenticatedUser_WhenNotAuthenticated_ShouldThrowDomainException()
        {
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(false);
            _currentUserService.Setup(x => x.UserId).Returns((Guid?)null);

            var ex = Assert.Throws<DomainException>(() =>
                AuditGuard.RequireAuthenticatedUser(_currentUserService.Object, "test action"));

            Assert.Contains("no authenticated user is available", ex.Message);
        }

        [Fact]
        public void RequireAuthenticatedUser_WhenUserIdMissing_ShouldThrowDomainException()
        {
            _currentUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserService.Setup(x => x.UserId).Returns((Guid?)null);

            Assert.Throws<DomainException>(() =>
                AuditGuard.RequireAuthenticatedUser(_currentUserService.Object, "test action"));
        }

        [Fact]
        public void RequireAuthenticatedUser_WhenServiceNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AuditGuard.RequireAuthenticatedUser(null!, "test action"));
        }
    }
}
