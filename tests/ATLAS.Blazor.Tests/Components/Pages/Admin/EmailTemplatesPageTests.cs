using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.EmailTemplates.Commands;
using ATLAS.Application.EmailTemplates.Queries;
using ATLAS.Blazor.Components.Pages.Admin;
using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class EmailTemplatesPageTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly EmailTemplate _template = new() { Name = "ApprovalNotification", Content = "Default body" };
    private readonly EmailTemplate _otherTemplate = new() { Name = "RejectionNotification", Content = "Rejection body" };

    public EmailTemplatesPageTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
        Services.AddSingleton(NullLogger<EmailTemplates>.Instance);
    }

    private void SetupTemplates()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetEmailTemplatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailTemplate> { _template, _otherTemplate });
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEmailTemplateByNameQuery>(q => q.Name == _template.Name), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_template);
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEmailTemplateByNameQuery>(q => q.Name == _otherTemplate.Name), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_otherTemplate);
    }

    private void SelectTemplate(IRenderedComponent<EmailTemplates> cut, EmailTemplate? template = null)
    {
        var target = template ?? _template;
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEmailTemplateByNameQuery>(q => q.Name == target.Name), It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        cut.FindAll("button").Single(b => b.TextContent.Contains(target.Name)).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template content", cut.Markup));
    }

    [Fact]
    public void EmailTemplates_WhenTemplateSelected_ShouldRenderResetButton()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        Assert.Contains("Reset to default", cut.Markup);
    }

    [Fact]
    public void ResetTemplate_ShouldSendResetCommand()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ResetEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Reset to default")).Click();

        _mediatorMock.Verify(
            m => m.Send(It.Is<ResetEmailTemplateCommand>(c => c.Name == _template.Name), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void EmailTemplates_Selector_ShouldShowTemplateNameAndDescription()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();

        // The selector row shows the template name and its useful description.
        Assert.Contains("ApprovalNotification", cut.Markup);
        Assert.Contains("Sent to the applicant when an application is approved.", cut.Markup);
    }

    [Fact]
    public void EmailTemplates_WhenTemplateSelected_ShouldRenderPlaceholdersTable()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        // Available placeholders are presented as a table with descriptions.
        Assert.Contains("Available placeholders", cut.Markup);
        Assert.Contains("ApplicationNumber", cut.Markup);
        Assert.Contains("The application reference number.", cut.Markup);
    }

    [Fact]
    public void EditMode_SelectingTemplate_ShouldRenderEditor()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        // Edit mode shows the template content editor.
        Assert.NotNull(cut.Find("#template-content"));
    }

    [Fact]
    public void EditMode_SelectingTemplate_ShouldRenderPlaceholdersAndActions()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        Assert.Contains("Available placeholders", cut.Markup);
        Assert.Contains("Save", cut.Markup);
        Assert.Contains("Preview", cut.Markup);
        Assert.Contains("Reset to default", cut.Markup);
    }

    [Fact]
    public void EditMode_Editor_ShouldHaveAutoSizingRowsAttribute()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        var editor = cut.Find("#template-content");
        Assert.NotNull(editor.GetAttribute("rows"));
    }

    [Fact]
    public void PreviewMode_ClickingPreview_ShouldCallPreviewQuery()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PreviewEmailTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rendered preview output");

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Preview")).Click();
        cut.WaitForAssertion(() => _mediatorMock.Verify(
            m => m.Send(It.IsAny<PreviewEmailTemplateQuery>(), It.IsAny<CancellationToken>()),
            Times.Once));
    }

    [Fact]
    public void PreviewMode_SuccessfulPreview_ShouldRenderPreviewAndHideEditor()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PreviewEmailTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rendered preview output");

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Preview")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Rendered preview output", cut.Markup));

        // Preview mode renders the preview, hides editor/placeholders/actions,
        // and shows the Edit template button.
        Assert.Contains("Preview — not sent", cut.Markup);
        Assert.Empty(cut.FindAll("#template-content"));
        Assert.DoesNotContain("Available placeholders", cut.Markup);
        Assert.DoesNotContain("Reset to default", cut.Markup);
        Assert.Contains("Edit template", cut.Markup);
    }

    [Fact]
    public void PreviewMode_PreviewDoesNotRenderEditor()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PreviewEmailTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rendered preview output");

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Preview")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Rendered preview output", cut.Markup));

        Assert.Empty(cut.FindAll("#template-content"));
    }

    [Fact]
    public void ReturningToEditMode_EditTemplate_ShouldRestoreEditorAndPreserveContent()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PreviewEmailTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rendered preview output");

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        // Edit the content, then preview.
        cut.Find("#template-content").Change("Edited draft content");
        cut.FindAll("button").Single(b => b.TextContent.Contains("Preview")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Rendered preview output", cut.Markup));

        // Return to edit mode.
        cut.FindAll("button").Single(b => b.TextContent.Contains("Edit template")).Click();

        // Editor, placeholders, and actions are restored; content preserved.
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#template-content")));
        Assert.Contains("Available placeholders", cut.Markup);
        Assert.Contains("Reset to default", cut.Markup);
        Assert.Contains("Edited draft content", cut.Markup);
    }

    [Fact]
    public void SelectingAnotherTemplate_FromPreview_ShouldReturnToEditModeForNewTemplate()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PreviewEmailTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rendered preview output");

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        // Enter preview mode.
        cut.FindAll("button").Single(b => b.TextContent.Contains("Preview")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Rendered preview output", cut.Markup));

        // Select another template.
        cut.FindAll("button").Single(b => b.TextContent.Contains(_otherTemplate.Name)).Click();
        cut.WaitForAssertion(() => Assert.Contains(_otherTemplate.Content, cut.Markup));

        // Back in edit mode: editor/placeholders/actions shown, previous preview gone.
        Assert.NotNull(cut.Find("#template-content"));
        Assert.Contains("Available placeholders", cut.Markup);
        Assert.Contains("Reset to default", cut.Markup);
        Assert.DoesNotContain("Rendered preview output", cut.Markup);
    }

    [Fact]
    public void PreviewFailure_ShouldRemainInEditMode_AndShowError()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PreviewEmailTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("preview failed"));

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Preview")).Click();
        cut.WaitForAssertion(() => Assert.Contains("We were unable to render the preview", cut.Markup));

        // Remains in edit mode: editor, placeholders, and actions still shown;
        // the Edit template button (preview mode only) is absent.
        Assert.NotNull(cut.Find("#template-content"));
        Assert.Contains("Available placeholders", cut.Markup);
        Assert.Contains("Reset to default", cut.Markup);
        Assert.Empty(cut.FindAll("button").Where(b => b.TextContent.Contains("Edit template")));
    }

    [Fact]
    public void SaveNotification_ShouldAppearAfterPlaceholders_AndBeforeActions()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Save")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template saved successfully.", cut.Markup));

        // The success notification comes after the placeholders section and before the actions.
        var availableIndex = cut.Markup.IndexOf("Available placeholders", System.StringComparison.Ordinal);
        var notificationIndex = cut.Markup.IndexOf("Template saved successfully.", System.StringComparison.Ordinal);
        var saveButtonIndex = cut.Markup.IndexOf("Save</button>", System.StringComparison.Ordinal);
        Assert.True(availableIndex > -1 && notificationIndex > availableIndex);
        Assert.True(notificationIndex < saveButtonIndex);
    }

    [Fact]
    public void ResetNotification_ShouldAppearAfterPlaceholders_AndBeforeActions()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ResetEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Reset to default")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template reset to its default.", cut.Markup));

        var availableIndex = cut.Markup.IndexOf("Available placeholders", System.StringComparison.Ordinal);
        var notificationIndex = cut.Markup.IndexOf("Template reset to its default.", System.StringComparison.Ordinal);
        var resetButtonIndex = cut.Markup.IndexOf("Reset to default", System.StringComparison.Ordinal);
        Assert.True(availableIndex > -1 && notificationIndex > availableIndex);
        Assert.True(notificationIndex < resetButtonIndex);
    }

    [Fact]
    public void Save_ShouldShowOnlySaveNotification()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Save")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template saved successfully.", cut.Markup));

        Assert.Single(cut.FindAll(".alert-success"));
        Assert.DoesNotContain("Template reset to its default.", cut.Markup);
    }

    [Fact]
    public void Reset_ShouldReplaceExistingSaveNotification()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ResetEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        // Save first.
        cut.FindAll("button").Single(b => b.TextContent.Contains("Save")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template saved successfully.", cut.Markup));

        // Then reset — the save notification must disappear.
        cut.FindAll("button").Single(b => b.TextContent.Contains("Reset to default")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template reset to its default.", cut.Markup));

        Assert.Single(cut.FindAll(".alert-success"));
        Assert.DoesNotContain("Template saved successfully.", cut.Markup);
    }

    [Fact]
    public void Save_ShouldReplaceExistingResetNotification()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ResetEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        // Reset first.
        cut.FindAll("button").Single(b => b.TextContent.Contains("Reset to default")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template reset to its default.", cut.Markup));

        // Then save — the reset notification must disappear.
        cut.FindAll("button").Single(b => b.TextContent.Contains("Save")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template saved successfully.", cut.Markup));

        Assert.Single(cut.FindAll(".alert-success"));
        Assert.DoesNotContain("Template reset to its default.", cut.Markup);
    }

    [Fact]
    public void Notifications_ShouldNotRenderInPreviewMode()
    {
        SetupTemplates();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PreviewEmailTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rendered preview output");

        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        // Save to set a success notification, then preview.
        cut.FindAll("button").Single(b => b.TextContent.Contains("Save")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template saved successfully.", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent.Contains("Preview")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Rendered preview output", cut.Markup));

        // Preview mode: notifications (and editor/placeholders) are not rendered.
        Assert.DoesNotContain("Template saved successfully.", cut.Markup);
        Assert.DoesNotContain("Available placeholders", cut.Markup);
        Assert.Contains("Edit template", cut.Markup);
    }
}
