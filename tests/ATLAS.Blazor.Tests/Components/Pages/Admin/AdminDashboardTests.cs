using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.Components.Pages.Admin;
using ATLAS.Blazor.Components.Shared.Admin;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class AdminDashboardTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public AdminDashboardTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static AdminDashboardDto SampleSummary() => new()
    {
        PermitTypeCount = 4,
        ApplicationCount = 12,
        OfficerCount = 3,
        AdminCount = 2,
        CitizenCount = 25,
        ActiveEmailTemplateCount = 0
    };

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<AdminDashboardDto>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default)).Returns(tcs.Task);

        var cut = Render<AdminDashboard>();

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderSummaryCards_WhenLoaded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        Assert.Equal(6, cut.FindAll(".card").Count);
        Assert.Contains("4", cut.Markup);
        Assert.Contains("12", cut.Markup);
        Assert.Contains("3", cut.Markup);
        Assert.Contains("2", cut.Markup);
        Assert.Contains("25", cut.Markup);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render<AdminDashboard>();

        Assert.NotNull(cut.Find(".alert-danger"));
    }

    [Fact]
    public void Should_RenderPageHeader()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        Assert.NotNull(cut.FindComponent<PageHeader>());
        Assert.Contains("Administration Dashboard", cut.Markup);
    }
}