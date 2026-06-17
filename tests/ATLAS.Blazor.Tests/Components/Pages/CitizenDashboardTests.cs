using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Blazor.Components.Pages;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class CitizenDashboardTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock;

    public CitizenDashboardTests()
    {
        _mediatorMock = new Mock<IMediator>();
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static List<CitizenDashboardDto> CreateSampleApplications()
    {
        return new List<CitizenDashboardDto>
        {
            new()
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationNumber = "APP-2026-0001",
                PermitTypeName = "Building Permit",
                Status = ApplicationStatus.Draft,
                SubmittedDate = null,
                LastUpdated = DateTime.UtcNow
            },
            new()
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationNumber = "APP-2026-0002",
                PermitTypeName = "Event Permit",
                Status = ApplicationStatus.Submitted,
                SubmittedDate = DateTime.UtcNow.AddDays(-2),
                LastUpdated = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationNumber = "APP-2026-0003",
                PermitTypeName = "Building Permit",
                Status = ApplicationStatus.Approved,
                SubmittedDate = DateTime.UtcNow.AddDays(-10),
                LastUpdated = DateTime.UtcNow.AddDays(-1)
            }
        };
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IEnumerable<CitizenDashboardDto>>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .Returns(tcs.Task);

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var spinner = cut.Find(".spinner-border");
        Assert.NotNull(spinner);
    }

    [Fact]
    public void Should_RenderApplications_WhenLoaded()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .ReturnsAsync(CreateSampleApplications());

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(3, rows.Count);
        Assert.Contains("APP-2026-0001", rows[0].TextContent);
        Assert.Contains("APP-2026-0002", rows[1].TextContent);
        Assert.Contains("APP-2026-0003", rows[2].TextContent);
    }

    [Fact]
    public void Should_ShowStatusBadge_ForEachApplication()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .ReturnsAsync(CreateSampleApplications());

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var badges = cut.FindAll(".badge");
        Assert.Equal(3, badges.Count);
        Assert.Contains("Draft", badges[0].TextContent);
        Assert.Contains("Submitted", badges[1].TextContent);
        Assert.Contains("Approved", badges[2].TextContent);
    }

    [Fact]
    public void Should_ShowEmptyState_WhenNoApplications()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .ReturnsAsync(new List<CitizenDashboardDto>());

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var alert = cut.Find(".alert-info");
        Assert.NotNull(alert);
        Assert.Contains("No permit applications found", alert.TextContent);
    }

    [Fact]
    public void Should_ShowApplyButton_WhenEmpty()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .ReturnsAsync(new List<CitizenDashboardDto>());

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var applyLink = cut.Find("a[href='/permits']");
        Assert.NotNull(applyLink);
        Assert.Contains("Apply for a Permit", applyLink.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.NotNull(alert);
        Assert.Contains("Something went wrong", alert.TextContent);
    }

    [Fact]
    public void Should_NavigateToEdit_ForDraftApplication()
    {
        // Arrange
        var apps = CreateSampleApplications();
        var draftAppId = apps[0].ApplicationId; // Draft
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .ReturnsAsync(apps);

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var actionLinks = cut.FindAll("tbody tr td a");
        var draftLink = actionLinks[0];
        Assert.EndsWith($"/applications/edit/{draftAppId}", draftLink.GetAttribute("href"));
        Assert.Contains("Continue Editing", draftLink.TextContent);
    }

    [Fact]
    public void Should_NavigateToDetail_ForNonDraftApplication()
    {
        // Arrange
        var apps = CreateSampleApplications();
        var submittedAppId = apps[1].ApplicationId; // Submitted
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .ReturnsAsync(apps);

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var actionLinks = cut.FindAll("tbody tr td a");
        var submittedLink = actionLinks[1];
        Assert.EndsWith($"/applications/{submittedAppId}", submittedLink.GetAttribute("href"));
        Assert.Contains("View Details", submittedLink.TextContent);
    }

    [Fact]
    public void Should_UseAccessibleButtonLabels()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCitizenDashboardQuery>(), default))
            .ReturnsAsync(CreateSampleApplications());

        // Act
        var cut = Render<CitizenDashboard>();

        // Assert
        var actionLinks = cut.FindAll("tbody tr td a");
        Assert.All(actionLinks, link =>
        {
            var ariaLabel = link.GetAttribute("aria-label");
            Assert.NotNull(ariaLabel);
        });
    }
}