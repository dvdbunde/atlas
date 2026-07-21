using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.AuditLogs;
using ATLAS.Blazor.Components.Pages.Admin;
using Bunit.TestDoubles;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class AuditLogsTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public AuditLogsTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static AuditLogListResult SampleResult(int count = 2)
    {
        var items = new List<AuditLogDto>();
        for (var i = 0; i < count; i++)
        {
            items.Add(new AuditLogDto
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Action = "Create",
                EntityType = "Application",
                EntityId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow
            });
        }
        return new AuditLogListResult { Items = items, TotalCount = count, PageNumber = 1, PageSize = 20 };
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<AuditLogListResult>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var cut = Render<AuditLogs>();

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderTableRows_WhenLogsLoaded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleResult(2));

        var cut = Render<AuditLogs>();

        Assert.Equal(2, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public void Should_ShowEmptyState_WhenNoLogs()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuditLogListResult { Items = new List<AuditLogDto>(), TotalCount = 0, PageNumber = 1, PageSize = 20 });

        var cut = Render<AuditLogs>();

        Assert.Contains("No audit entries found", cut.Markup);
    }

    [Fact]
    public void Should_ShowError_WhenLoadFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render<AuditLogs>();

        Assert.Contains("unable to load audit logs", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_PassSearchTerm_ToQuery()
    {
        GetAuditLogsQuery? captured = null;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((q, _) => captured = (GetAuditLogsQuery)q)
            .ReturnsAsync(SampleResult(1));

        var cut = Render<AuditLogs>();
        var input = cut.Find("input[aria-label=\x22Search audit logs\x22]");
        input.Input("login");

        Assert.NotNull(captured);
        Assert.Equal("login", captured!.SearchTerm);
    }
}



