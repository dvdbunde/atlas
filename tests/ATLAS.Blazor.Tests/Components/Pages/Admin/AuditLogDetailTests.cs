using System;
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

public class AuditLogDetailTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public AuditLogDetailTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static AuditLogDto Sample(Guid id) => new()
    {
        Id = id,
        UserId = Guid.NewGuid(),
        Action = "Update",
        EntityType = "Permit",
        EntityId = Guid.NewGuid(),
        Details = "Permit updated",
        Timestamp = DateTime.UtcNow,
        IpAddress = "127.0.0.1"
    };

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<AuditLogDto?>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var cut = Render<AuditLogDetail>(parameters => parameters.Add(p => p.Id, Guid.NewGuid()));

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderDetail_WhenFound()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Sample(id));

        var cut = Render<AuditLogDetail>(parameters => parameters.Add(p => p.Id, id));

        Assert.Contains("Permit updated", cut.Markup);
        Assert.Contains("Update", cut.Markup);
    }

    [Fact]
    public void Should_ShowNotFound_WhenMissing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLogDto?)null);

        var cut = Render<AuditLogDetail>(parameters => parameters.Add(p => p.Id, Guid.NewGuid()));

        Assert.Contains("not found", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_ShowError_WhenLoadFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render<AuditLogDetail>(parameters => parameters.Add(p => p.Id, Guid.NewGuid()));

        Assert.Contains("unable to load", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_DisplayUserNameAndEmail_WhenResolved()
    {
        var id = Guid.NewGuid();
        var dto = Sample(id);
        dto.UserName = "Jane Doe";
        dto.UserEmail = "jane@example.com";
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var cut = Render<AuditLogDetail>(parameters => parameters.Add(p => p.Id, id));

        Assert.Contains("Jane Doe", cut.Markup);
        Assert.Contains("jane@example.com", cut.Markup);
    }

    [Fact]
    public void Should_NotDisplayRawUserId_WhenUserNameResolved()
    {
        var id = Guid.NewGuid();
        var dto = Sample(id);
        dto.UserName = "Jane Doe";
        dto.UserEmail = "jane@example.com";
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var cut = Render<AuditLogDetail>(parameters => parameters.Add(p => p.Id, id));

        Assert.DoesNotContain(dto.UserId.ToString(), cut.Markup);
    }

    [Fact]
    public void Should_NotDisplayIpAddress()
    {
        var id = Guid.NewGuid();
        var dto = Sample(id);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var cut = Render<AuditLogDetail>(parameters => parameters.Add(p => p.Id, id));

        Assert.DoesNotContain("IP Address", cut.Markup);
        Assert.DoesNotContain("127.0.0.1", cut.Markup);
    }

    [Fact]
    public void Should_DisplaySystem_WhenUserUnresolved()
    {
        var id = Guid.NewGuid();
        var dto = Sample(id);
        dto.UserName = string.Empty;
        dto.UserEmail = string.Empty;
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var cut = Render<AuditLogDetail>(parameters => parameters.Add(p => p.Id, id));

        Assert.Contains("System", cut.Markup);
    }
}
