using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Admin;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries.Admin;

public class GetUserByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenUserNotFound()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var auditRepo = new Mock<IAuditLogRepository>();

        var handler = new GetUserByIdQueryHandler(userRepo.Object, auditRepo.Object);
        var result = await handler.Handle(new GetUserByIdQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnDetail_WithRecentAuditEntries()
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, "amy@atlas.test", "Amy", "Admin", UserRole.Admin);
        user.RecordLogin();

        var auditEntries = new List<AuditLog>
        {
            new(userId, "Login", "User", userId, "details", "10.0.0.1"),
            new(userId, "ViewPermit", "Permit", Guid.NewGuid(), "details", "10.0.0.1")
        };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(auditEntries);

        var handler = new GetUserByIdQueryHandler(userRepo.Object, auditRepo.Object);
        var result = await handler.Handle(new GetUserByIdQuery { UserId = userId }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        Assert.Equal("Amy Admin", result.FullName);
        Assert.Equal(UserRole.Admin, result.Role);
        Assert.Equal(2, result.RecentAuditEntries.Count);
        Assert.Contains(result.RecentAuditEntries, e => e.Action == "Login");
        Assert.Contains(result.RecentAuditEntries, e => e.Action == "ViewPermit");
    }

    [Fact]
    public async Task Handle_ShouldReturnDetail_WithEmptyAudit_WhenNoEntries()
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, "amy@atlas.test", "Amy", "Admin", UserRole.Admin);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AuditLog>());

        var handler = new GetUserByIdQueryHandler(userRepo.Object, auditRepo.Object);
        var result = await handler.Handle(new GetUserByIdQuery { UserId = userId }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.RecentAuditEntries);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRepositoriesAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => new GetUserByIdQueryHandler(null!, new Mock<IAuditLogRepository>().Object));
        Assert.Throws<ArgumentNullException>(() => new GetUserByIdQueryHandler(new Mock<IUserRepository>().Object, null!));
    }
}
