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

public class GetUsersQueryHandlerTests
{
    private static User MakeUser(string first, string last, string email, UserRole role, DateTime? lastLogin = null)
    {
        var user = new User(Guid.NewGuid(), email, first, last, role);
        if (lastLogin.HasValue)
            user.RecordLogin();
        return user;
    }

    [Fact]
    public async Task Handle_ShouldReturnAllUsers_WhenNoFilters()
    {
        var users = new List<User>
        {
            MakeUser("Amy", "Admin", "amy@atlas.test", UserRole.Admin, DateTime.UtcNow.AddDays(-1)),
            MakeUser("Owen", "Officer", "owen@atlas.test", UserRole.Officer, DateTime.UtcNow.AddDays(-2)),
            MakeUser("Cara", "Citizen", "cara@atlas.test", UserRole.Citizen, null)
        };

        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new GetUsersQueryHandler(repo.Object);
        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task Handle_ShouldFilterByRole()
    {
        var users = new List<User>
        {
            MakeUser("Amy", "Admin", "amy@atlas.test", UserRole.Admin),
            MakeUser("Owen", "Officer", "owen@atlas.test", UserRole.Officer)
        };

        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new GetUsersQueryHandler(repo.Object);
        var result = await handler.Handle(new GetUsersQuery { Role = UserRole.Officer }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Owen", result.Items[0].FirstName);
    }

    [Fact]
    public async Task Handle_ShouldSearchByNameAndEmail_CaseInsensitive()
    {
        var users = new List<User>
        {
            MakeUser("Amy", "Admin", "amy@atlas.test", UserRole.Admin),
            MakeUser("Owen", "Officer", "owen@atlas.test", UserRole.Officer)
        };

        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new GetUsersQueryHandler(repo.Object);
        var byName = await handler.Handle(new GetUsersQuery { SearchTerm = "owen" }, CancellationToken.None);
        var byEmail = await handler.Handle(new GetUsersQuery { SearchTerm = "AMY@ATLAS" }, CancellationToken.None);

        Assert.Single(byName.Items);
        Assert.Single(byEmail.Items);
    }

    [Fact]
    public async Task Handle_ShouldSortByNameAscByDefault()
    {
        var users = new List<User>
        {
            MakeUser("Zoe", "Z", "z@atlas.test", UserRole.Citizen),
            MakeUser("Amy", "A", "a@atlas.test", UserRole.Citizen)
        };

        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new GetUsersQueryHandler(repo.Object);
        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        Assert.Equal("Amy", result.Items[0].FirstName);
        Assert.Equal("Zoe", result.Items[1].FirstName);
    }

    [Fact]
    public async Task Handle_ShouldPageResults()
    {
        var users = Enumerable.Range(0, 25)
            .Select(i => MakeUser($"User{i}", "Test", $"user{i}@atlas.test", UserRole.Citizen))
            .ToList();

        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new GetUsersQueryHandler(repo.Object);
        var page1 = await handler.Handle(new GetUsersQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);
        var page3 = await handler.Handle(new GetUsersQuery { PageNumber = 3, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(5, page3.Items.Count);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new GetUsersQueryHandler(null!));
    }
}
