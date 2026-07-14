using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.API.Controllers;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ATLAS.API.Tests.Controllers;

public class OfficerDashboardControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ApplicationsController _controller;

    public OfficerDashboardControllerTests()
    {
        _controller = new ApplicationsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task OfficerDashboard_ShouldReturnOk_WithMappedResult()
    {
        var result = new OfficerDashboardResult
        {
            Items = new List<OfficerDashboardDto>
            {
                new() { ApplicationId = Guid.NewGuid(), ApplicationNumber = "APP-1", Status = ApplicationStatus.Submitted }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.DashboardGet();
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var returned = Assert.IsType<OfficerDashboardResult>(ok.Value);

        Assert.Equal(1, returned.TotalCount);
        Assert.Single(returned.Items);
    }

    [Fact]
    public void OfficerDashboard_ShouldRequireOfficerOrAdminPolicy()
    {
        var method = typeof(ApplicationsController).GetMethod("DashboardGet");
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attr);
        Assert.Equal("OfficerOrAdmin", attr!.Policy);
    }
}