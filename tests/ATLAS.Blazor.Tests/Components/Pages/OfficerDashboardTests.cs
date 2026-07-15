using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class OfficerDashboardTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public OfficerDashboardTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static OfficerDashboardResult SampleResult(int count = 3)
    {
        var items = new List<OfficerDashboardDto>();
        for (int i = 0; i < count; i++)
        {
            items.Add(new OfficerDashboardDto
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationNumber = $"APP-2026-{i:D4}",
                PermitTypeName = "Building Permit",
                Status = ApplicationStatus.Submitted,
                CitizenName = "John Smith",
                SubmittedDate = DateTime.UtcNow.AddDays(-i),
                LastUpdated = DateTime.UtcNow.AddDays(-i),
                AssignedOfficerName = null,
                DocumentCount = 1,
                AllRequiredDocumentsUploaded = true
            });
        }
        return new OfficerDashboardResult
        {
            Items = items,
            TotalCount = count,
            PageNumber = 1,
            PageSize = 12
        };
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<OfficerDashboardResult>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default)).Returns(tcs.Task);

        var cut = Render<OfficerDashboard>();

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderCards_WhenLoaded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default))
            .ReturnsAsync(SampleResult(3));
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        var cut = Render<OfficerDashboard>();

        Assert.Equal(3, cut.FindAll(".card").Count);
        Assert.Contains("APP-2026-0000", cut.Markup);
    }

    [Fact]
    public void Should_ShowStatusBadge_ForEachCard()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default))
            .ReturnsAsync(SampleResult(2));
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        var cut = Render<OfficerDashboard>();

        Assert.Equal(2, cut.FindAll(".badge").Count(b => b.TextContent.Contains("Submitted")));
    }

    [Fact]
    public void Should_ShowEmptyState_WhenNoApplications()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default))
            .ReturnsAsync(new OfficerDashboardResult { Items = new List<OfficerDashboardDto>(), TotalCount = 0 });
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        var cut = Render<OfficerDashboard>();

        Assert.NotNull(cut.Find(".alert-info"));
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        var cut = Render<OfficerDashboard>();

        Assert.NotNull(cut.Find(".alert-danger"));
    }

    [Fact]
    public void Should_NavigateToApplication_ForEachCard()
    {
        var result = SampleResult(1);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default)).ReturnsAsync(result);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        var cut = Render<OfficerDashboard>();

        var link = cut.Find("a.btn-primary");
        Assert.EndsWith($"/officer/applications/{result.Items[0].ApplicationId}", link.GetAttribute("href"));
        Assert.Contains("Open Application", link.TextContent);
    }

    [Fact]
    public void Should_RenderPagination_WhenMultiplePages()
    {
        var result = new OfficerDashboardResult
        {
            Items = SampleResult(12).Items,
            TotalCount = 25,
            PageNumber = 1,
            PageSize = 12
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default)).ReturnsAsync(result);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        var cut = Render<OfficerDashboard>();

        Assert.Contains("Page 1 of 3", cut.Markup);
    }

    [Fact]
    public void Should_Requery_WhenFilterChanges()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default))
            .ReturnsAsync(SampleResult(1));
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        var cut = Render<OfficerDashboard>();

        // Change status filter -> triggers a new Send
        var select = cut.Find("select");
        select.Change("UnderReview");

        _mediatorMock.Verify(m => m.Send(It.IsAny<GetOfficerDashboardQuery>(), default), Times.AtLeast(2));
    }
}