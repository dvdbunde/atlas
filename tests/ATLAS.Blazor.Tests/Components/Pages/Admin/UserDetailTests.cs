using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.Components.Pages.Admin;
using Bunit.TestDoubles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class UserDetailTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public UserDetailTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static UserDetailDto SampleUser(Guid id) => new()
    {
        Id = id,
        FirstName = "Amy",
        LastName = "Admin",
        FullName = "Amy Admin",
        Email = "amy@atlas.test",
        Role = ATLAS.Domain.Entities.UserRole.Admin,
        LastLoginDate = DateTime.UtcNow.AddDays(-1),
        RecentAuditEntries = new List<AuditLogDto>
        {
            new() { Id = Guid.NewGuid(), UserId = id, Action = "Login", EntityType = "User", Timestamp = DateTime.UtcNow }
        }
    };

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<UserDetailDto?>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), default)).Returns(tcs.Task);

        var cut = Render<UserDetail>(parameters => parameters.Add(p => p.Id, Guid.NewGuid()));

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderDetail_WhenUserFound()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), default))
            .ReturnsAsync(SampleUser(id));

        var cut = Render<UserDetail>(parameters => parameters.Add(p => p.Id, id));

        Assert.Contains("Amy Admin", cut.Markup);
        Assert.Contains("amy@atlas.test", cut.Markup);
        Assert.Contains("Admin", cut.Markup);
        Assert.Contains("Recent Activity", cut.Markup);
        Assert.Contains("Login", cut.Markup);
    }

    [Fact]
    public void Should_ShowNotFound_WhenUserMissing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), default))
            .ReturnsAsync((UserDetailDto?)null);

        var cut = Render<UserDetail>(parameters => parameters.Add(p => p.Id, Guid.NewGuid()));

        Assert.Contains("User not found", cut.Markup);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render<UserDetail>(parameters => parameters.Add(p => p.Id, Guid.NewGuid()));

        Assert.NotNull(cut.Find(".alert-danger"));
    }

    [Fact]
    public void Should_NotRenderRoleEditControl()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), default))
            .ReturnsAsync(SampleUser(id));

        var cut = Render<UserDetail>(parameters => parameters.Add(p => p.Id, id));

        // Role is read-only: no select/input for role editing
        Assert.DoesNotContain("<select", cut.Markup);
    }

    [Fact]
    public void Should_DeclareAdminRoleAuthorization()
    {
        var attribute = typeof(UserDetail)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("Admin", attribute.Roles);
    }
}
