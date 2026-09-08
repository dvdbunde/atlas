//----------------------
// Operations page tests (O5 - Operations Portal)
// Verifies loading, healthy, degraded/unhealthy, unavailable-data and error
// rendering states using bUnit with a mocked MediatR.
//----------------------

using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.Components.Pages.Admin;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class OperationsTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public OperationsTests()
    {
        Services.AddSingleton(_mediatorMock.Object);

        // Provide the non-secret Monitoring configuration used by the Deeper
        // Telemetry block (Grafana Cloud dashboards URL).
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Monitoring:GrafanaDashboardsUrl"] = "https://atlasmonitoringdvb.grafana.net/dashboards"
            })
            .Build();
        Services.AddSingleton<IConfiguration>(config);
    }

    private static OperationsOverviewDto SampleOverview(
        string overall = "Healthy",
        bool healthUnavailable = false,
        IReadOnlyDictionary<string, long>? transitions = null,
        IReadOnlyDictionary<string, long>? emailSends = null) => new()
    {
        OverallHealth = healthUnavailable ? null : overall,
        HealthUnavailable = healthUnavailable,
        HealthEntries = healthUnavailable
            ? Array.Empty<OperationsHealthEntryDto>()
            : new[]
            {
                new OperationsHealthEntryDto { Name = "database", Status = "Healthy" },
                new OperationsHealthEntryDto { Name = "key-vault", Status = "Unhealthy", Description = "Vault unreachable" }
            },
        ApplicationTransitions = transitions,
        EmailSends = emailSends,
        CapturedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<OperationsOverviewDto>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), default)).Returns(tcs.Task);

        var cut = Render<Operations>();

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderHealthEntries_WhenLoaded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), default))
            .ReturnsAsync(SampleOverview());

        var cut = Render<Operations>();

        Assert.Contains("database", cut.Markup);
        Assert.Contains("key-vault", cut.Markup);
        Assert.Contains("Vault unreachable", cut.Markup);
        Assert.Contains("Healthy", cut.Markup);
        Assert.Contains("Unhealthy", cut.Markup);
    }

    [Fact]
    public void Should_ShowUnavailable_WhenHealthDataCannotBeRetrieved()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), default))
            .ReturnsAsync(SampleOverview(healthUnavailable: true));

        var cut = Render<Operations>();

        // Explicitly unavailable - not silently zero or "Unhealthy".
        Assert.Contains("Health data unavailable", cut.Markup);
    }

    [Fact]
    public void Should_RenderActivityCounts_WhenMetricsExist()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), default))
            .ReturnsAsync(SampleOverview(
                transitions: new Dictionary<string, long> { ["submitted"] = 7 },
                emailSends: new Dictionary<string, long> { ["success"] = 9, ["failure"] = 2 }));

        var cut = Render<Operations>();

        Assert.Contains("submitted", cut.Markup);
        Assert.Contains("7", cut.Markup);
        Assert.Contains("9", cut.Markup);
        Assert.Contains("2", cut.Markup);
    }

    [Fact]
    public void Should_ShowNoActivityMessage_WhenNoMetricsRecorded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), default))
            .ReturnsAsync(SampleOverview());

        var cut = Render<Operations>();

        // Null metrics (no data) must not render as zero.
        Assert.Contains("No activity recorded yet", cut.Markup);
        Assert.Contains("No email activity recorded yet", cut.Markup);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("telemetry failure"));

        var cut = Render<Operations>();

        Assert.Contains("Something went wrong", cut.Markup);
        Assert.Contains("Try Again", cut.Markup);
    }

    [Fact]
    public void Should_RenderDeeperTelemetryLinks_FromConfiguration()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), default))
            .ReturnsAsync(SampleOverview());

        var cut = Render<Operations>();

        // Azure Portal link.
        Assert.NotNull(cut.Find("a[href='https://portal.azure.com/']"));
        // Grafana Cloud dashboards link, sourced from configuration.
        Assert.NotNull(cut.Find("a[href='https://atlasmonitoringdvb.grafana.net/dashboards']"));
        // Azure Managed Grafana must not be referenced.
        Assert.DoesNotContain("Azure Managed Grafana", cut.Markup);
    }
}
