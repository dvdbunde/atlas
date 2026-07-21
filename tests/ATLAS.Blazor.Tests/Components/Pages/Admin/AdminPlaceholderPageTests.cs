using ATLAS.Blazor.Components.Pages.Admin;
using ATLAS.Blazor.Components.Shared.Admin;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class AdminPlaceholderPageTests : BunitContext
{
    [Fact]
    public void DynamicForms_ShouldRenderHeaderAndEmptyState()
    {
        var cut = Render<DynamicForms>();

        Assert.NotNull(cut.FindComponent<PageHeader>());
        Assert.NotNull(cut.FindComponent<EmptyState>());
        Assert.Contains("Dynamic Forms", cut.Markup);
    }

    [Fact]
    public void EmailTemplates_ShouldRenderHeaderAndEmptyState()
    {
        var cut = Render<EmailTemplates>();

        Assert.NotNull(cut.FindComponent<PageHeader>());
        Assert.NotNull(cut.FindComponent<EmptyState>());
        Assert.Contains("Email Templates", cut.Markup);
    }

    [Fact]
    public void ReferenceData_ShouldRenderHeaderAndEmptyState()
    {
        var cut = Render<ReferenceData>();

        Assert.NotNull(cut.FindComponent<PageHeader>());
        Assert.NotNull(cut.FindComponent<EmptyState>());
        Assert.Contains("Reference Data", cut.Markup);
    }

    [Fact]
    public void Officers_ShouldRenderHeaderAndEmptyState()
    {
        var cut = Render<Officers>();

        Assert.NotNull(cut.FindComponent<PageHeader>());
        Assert.NotNull(cut.FindComponent<EmptyState>());
        Assert.Contains("Officers", cut.Markup);
    }

    [Fact]
    public void SystemSettings_ShouldRenderHeaderAndEmptyState()
    {
        var cut = Render<SystemSettings>();

        Assert.NotNull(cut.FindComponent<PageHeader>());
        Assert.NotNull(cut.FindComponent<EmptyState>());
        Assert.Contains("System Settings", cut.Markup);
    }
}
