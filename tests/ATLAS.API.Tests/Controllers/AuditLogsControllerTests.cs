using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.API.Contracts.Generated;
using ATLAS.API.Controllers;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.AuditLogs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ATLAS.API.Tests.Controllers;

public class AuditLogsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AuditLogsController _controller;

    public AuditLogsControllerTests()
    {
        _controller = new AuditLogsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnOk_WithPagedResponse()
    {
        var result = new AuditLogListResult
        {
            Items = new List<AuditLogDto>
            {
                new() { Id = Guid.NewGuid(), Action = "Create", EntityType = "Application", EntityId = Guid.NewGuid(), Timestamp = DateTime.UtcNow }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetAuditLogs();
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var returned = Assert.IsType<PagedAuditLogResponse>(ok.Value);

        Assert.Equal(1, returned.TotalCount);
        Assert.Single(returned.Items);
        Assert.Equal("Create", returned.Items.First().Action);
    }

    [Fact]
    public async Task GetAuditLogById_ShouldReturnNotFound_WhenMissing()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuditLogDto?)null);

        var action = await _controller.GetAuditLogById(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(action.Result);
    }

    [Fact]
    public async Task GetAuditLogById_ShouldReturnOk_WhenFound()
    {
        var dto = new AuditLogDto { Id = Guid.NewGuid(), Action = "Update", EntityType = "Permit", EntityId = Guid.NewGuid(), Timestamp = DateTime.UtcNow };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAuditLogDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var action = await _controller.GetAuditLogById(dto.Id);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var returned = Assert.IsType<AuditLogResponse>(ok.Value);

        Assert.Equal(dto.Id, returned.Id);
        Assert.Equal("Update", returned.Action);
    }
}
