using ATLAS.Blazor.Components.Shared;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Shared;

public class ApplicationSummaryCardTests : BunitContext
{
    private static OfficerApplicationCardViewModel Sample() => new()
    {
        ApplicationId = Guid.NewGuid(),
        ApplicationNumber = "APP-2026-0001",
        PermitTypeName = "Building Permit",
        Status = ApplicationStatus.UnderReview,
        CitizenName = "John Smith",
        SubmittedDate = DateTime.UtcNow.AddDays(-3),
        LastUpdated = DateTime.UtcNow.AddDays(-1),
        AssignedOfficerName = "Jane Doe",
        DocumentCount = 2,
        AllRequiredDocumentsUploaded = true
    };

    [Fact]
    public void Should_RenderKeyFields()
    {
        var cut = Render<ApplicationSummaryCard>(parameters => parameters
            .Add(p => p.Application, Sample()));

        Assert.Contains("Building Permit", cut.Markup);
        Assert.Contains("APP-2026-0001", cut.Markup);
        Assert.Contains("John Smith", cut.Markup);
        Assert.Contains("Jane Doe", cut.Markup);
    }

    [Fact]
    public void Should_RenderOpenApplicationLink()
    {
        var vm = Sample();
        var cut = Render<ApplicationSummaryCard>(parameters => parameters
            .Add(p => p.Application, vm));

        var link = cut.Find("a.btn-primary");
        Assert.EndsWith($"/officer/review/{vm.ApplicationId}", link.GetAttribute("href"));
        Assert.Contains("Open Application", link.TextContent);
    }

    [Fact]
    public void Should_ShowAllRequiredDocsBadge_WhenComplete()
    {
        var cut = Render<ApplicationSummaryCard>(parameters => parameters
            .Add(p => p.Application, Sample()));

        Assert.Contains("All required docs", cut.Markup);
    }

    [Fact]
    public void Should_ShowIncompleteBadge_WhenMissingDocs()
    {
        var baseVm = Sample();
        var vm = new OfficerApplicationCardViewModel
        {
            ApplicationId = baseVm.ApplicationId,
            ApplicationNumber = baseVm.ApplicationNumber,
            PermitTypeName = baseVm.PermitTypeName,
            Status = baseVm.Status,
            CitizenName = baseVm.CitizenName,
            SubmittedDate = baseVm.SubmittedDate,
            LastUpdated = baseVm.LastUpdated,
            AssignedOfficerName = baseVm.AssignedOfficerName,
            DocumentCount = baseVm.DocumentCount,
            AllRequiredDocumentsUploaded = false
        };
        var cut = Render<ApplicationSummaryCard>(parameters => parameters
            .Add(p => p.Application, vm));

        Assert.Contains("Docs incomplete", cut.Markup);
    }

    [Fact]
    public void Should_ShowUnassigned_WhenNoOfficer()
    {
        var baseVm = Sample();
        var vm = new OfficerApplicationCardViewModel
        {
            ApplicationId = baseVm.ApplicationId,
            ApplicationNumber = baseVm.ApplicationNumber,
            PermitTypeName = baseVm.PermitTypeName,
            Status = baseVm.Status,
            CitizenName = baseVm.CitizenName,
            SubmittedDate = baseVm.SubmittedDate,
            LastUpdated = baseVm.LastUpdated,
            AssignedOfficerName = null,
            DocumentCount = baseVm.DocumentCount,
            AllRequiredDocumentsUploaded = baseVm.AllRequiredDocumentsUploaded
        };
        var cut = Render<ApplicationSummaryCard>(parameters => parameters
            .Add(p => p.Application, vm));

        Assert.Contains("Unassigned", cut.Markup);
    }
}