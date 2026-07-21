using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.Components.Pages.Admin;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class UsersListTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public UsersListTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static List<UserSummaryDto> SampleUsers() => new()
    {
        new UserSummaryDto { Id = Guid.NewGuid(), FirstName = "Amy", LastName = "Admin", FullName = "Amy Admin", Email = "amy@atlas.test", Role = ATLAS.Domain.Entities.UserRole.Admin, LastLoginDate = DateTime.UtcNow.AddDays(-1) },
        new UserSummaryDto { Id = Guid.NewGuid(), FirstName = "Owen", LastName = "Officer", FullName = "Owen Officer", Email = "owen@atlas.test", Role = ATLAS.Domain.Entities.UserRole.Officer, LastLoginDate = null }
    };

    private static UserListResult Result(List<UserSummaryDto> users, int page = 1, int totalCount = 0)
        => new() { Items = users, TotalCount = totalCount == 0 ? users.Count : totalCount, PageNumber = page, PageSize = 20 };

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<UserListResult>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), default)).Returns(tcs.Task);

        var cut = Render<Users>();

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderTableRows_WhenLoaded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), default))
            .ReturnsAsync(Result(SampleUsers()));

        var cut = Render<Users>();

        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Contains("Amy Admin", cut.Markup);
        Assert.Contains("Officer", cut.Markup);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render<Users>();

        Assert.NotNull(cut.Find(".alert-danger"));
    }

    [Fact]
    public void Should_ShowEmptyState_WhenNoUsers()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), default))
            .ReturnsAsync(Result(new List<UserSummaryDto>()));

        var cut = Render<Users>();

        Assert.Contains("No users found", cut.Markup);
    }

    [Fact]
    public void Should_RenderPagination_WhenMultiplePages()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), default))
            .ReturnsAsync(Result(SampleUsers(), page: 1, totalCount: 25));

        var cut = Render<Users>();

        Assert.NotNull(cut.Find(".pagination"));
        Assert.Contains("Page 1 of 2", cut.Markup);
    }

    [Fact]
    public void Should_DisplayRoleAsReadOnlyBadge_NotEditableControl()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), default))
            .ReturnsAsync(Result(SampleUsers()));

        var cut = Render<Users>();

        // Role is rendered as a read-only badge in the table, and there is no
        // "Change Role" / activation control on the list.
        Assert.Contains("badge", cut.Markup);
        Assert.DoesNotContain("Change Role", cut.Markup);
        Assert.DoesNotContain("Activate", cut.Markup);
        Assert.DoesNotContain("Deactivate", cut.Markup);
    }
}
