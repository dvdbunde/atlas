using System;
using System.Collections.Generic;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Admin;
using ATLAS.Application.Queries.AuditLogs;
using ATLAS.Blazor.Components.Pages.Admin;
using ATLAS.Blazor.Components.Shared.Admin;
using ATLAS.Domain.Enums;
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
        ActivePermitTypeCount = 3,
        InactivePermitTypeCount = 1,
        ApplicationCount = 12,
        ApplicationStatusCounts = new Dictionary<ApplicationStatus, int>
        {
            [ApplicationStatus.Submitted] = 5,
            [ApplicationStatus.UnderReview] = 4
        },
        UserCount = 30,
        OfficerCount = 3,
        AdminCount = 2,
        CitizenCount = 25,
        AuditLogEventsLast24Hours = 8,
        LatestAuditEventUtc = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc),
        EmailTemplateCount = 4,
        OverallHealth = "Healthy",
        HealthCheckedAtUtc = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc)
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
    public void Should_RenderSixSummaryBlocks_WhenLoaded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        Assert.Equal(6, cut.FindAll(".card").Count);
    }

    [Fact]
    public void Should_RenderBlockTitlesWithTotals()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        Assert.Contains("Permit Types: 4", cut.Markup);
        Assert.Contains("Applications: 12", cut.Markup);
        Assert.Contains("Users: 30", cut.Markup);
        Assert.Contains("Email Templates: 4", cut.Markup);
        Assert.Contains("Audit Logs", cut.Markup);
        Assert.Contains("Operations", cut.Markup);
    }

    [Fact]
    public void Should_RenderPermitTypeBodyCounts()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        Assert.Contains("Active", cut.Markup);
        Assert.Contains("Inactive", cut.Markup);
    }

    [Fact]
    public void Should_RenderUserBreakdown()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        Assert.Contains("Citizens", cut.Markup);
        Assert.Contains("Officers", cut.Markup);
        Assert.Contains("Administrators", cut.Markup);
        Assert.Contains("25", cut.Markup); // citizens
        Assert.Contains("3", cut.Markup);  // officers
        Assert.Contains("2", cut.Markup);  // admins
    }

    [Fact]
    public void Should_RenderNonZeroApplicationStatusesOnly()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        // Present statuses.
        Assert.Contains("Under Review", cut.Markup);
        Assert.Contains("Submitted", cut.Markup);
        // A zero-count status (e.g. Approved) must not be rendered.
        Assert.DoesNotContain("Approved", cut.Markup);
        Assert.DoesNotContain("Rejected", cut.Markup);
    }

    [Fact]
    public void Should_RenderAuditAndOperationsMetrics()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        Assert.Contains("Events (last 24h)", cut.Markup);
        Assert.Contains("System health", cut.Markup);
        Assert.Contains("Healthy", cut.Markup);
    }

    [Fact]
    public void EachBlock_ShouldNavigateToCorrespondingAdminPage()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), default))
            .ReturnsAsync(SampleSummary());

        var cut = Render<AdminDashboard>();

        Assert.NotNull(cut.Find($"a[href='/admin/permit-types']"));
        Assert.NotNull(cut.Find($"a[href='/admin/applications']"));
        Assert.NotNull(cut.Find($"a[href='/admin/users']"));
        Assert.NotNull(cut.Find($"a[href='/admin/audit-logs']"));
        Assert.NotNull(cut.Find($"a[href='/admin/email-templates']"));
        Assert.NotNull(cut.Find($"a[href='/admin/operations']"));
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