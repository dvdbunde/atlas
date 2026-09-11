using ATLAS.Application.EmailTemplates;
using ATLAS.Application.EmailTemplates.Commands;
using ATLAS.Application.EmailTemplates.Queries;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class EmailTemplates : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<EmailTemplates> Logger { get; set; } = default!;

    private bool _dataLoaded;
    private bool _isLoading;
    private bool _hasError;
    private bool _notFound;
    private string? _errorMessage;

    private IReadOnlyList<EmailTemplate> _templates = new List<EmailTemplate>();

    // Editor state for the currently selected template.
    private string? _selectedName;
    private string _editorContent = string.Empty;
    private bool _isSaving;
    private string? _saveMessage;
    private string? _saveError;

    // Reset state.
    private bool _isResetting;
    private string? _resetMessage;
    private string? _resetError;

    // Preview state.
    // _isPreviewing represents the asynchronous preview-generation operation.
    private bool _isPreviewing;
    // _isPreviewMode is the persistent UI mode that determines whether the page
    // shows the rendered preview (true) or the editing workspace (false).
    private bool _isPreviewMode;
    private string? _previewOutput;
    private string? _previewError;

    private IReadOnlyList<string> SupportedPlaceholders => KnownEmailPlaceholders.All;

    // Fixed, human-friendly display descriptions for the application-owned templates.
    // The templates themselves are fixed; these descriptions are presentation only.
    private static IReadOnlyDictionary<string, string> TemplateDescriptions { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [KnownEmailTemplates.SubmissionConfirmation] = "Sent to the applicant when an application is submitted.",
            [KnownEmailTemplates.ReSubmissionConfirmation] = "Sent to the applicant when a draft is resubmitted.",
            [KnownEmailTemplates.ApprovalNotification] = "Sent to the applicant when an application is approved.",
            [KnownEmailTemplates.RejectionNotification] = "Sent to the applicant when an application is rejected.",
            [KnownEmailTemplates.InfoRequestNotification] = "Sent to the applicant when additional information is requested."
        };

    // Fixed, human-friendly descriptions for the supported placeholders.
    private static IReadOnlyDictionary<string, string> PlaceholderDescriptions { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ApplicationNumber"] = "The application reference number.",
            ["PermitTypeName"] = "The name of the permit type.",
            ["Status"] = "The current application status.",
            ["CitizenName"] = "The applicant's full name.",
            ["Message"] = "The message from the officer.",
            ["ReasonCode"] = "The rejection reason code."
        };

    private string TemplateDescription(EmailTemplate template) =>
        TemplateDescriptions.TryGetValue(template.Name, out var description)
            ? description
            : "System email template.";

    private string PlaceholderDescription(string placeholder) =>
        PlaceholderDescriptions.TryGetValue(placeholder, out var description)
            ? description
            : string.Empty;

    // Auto-sizes the editor vertically based on the current template content so
    // normal templates are visible without an internal scrollbar. Uses a sensible
    // minimum for short templates and a cap so the editor never becomes enormous.
    private int EditorRows
    {
        get
        {
            if (string.IsNullOrEmpty(_editorContent))
                return 6;

            var lines = _editorContent.Split('\n').Length;
            return Math.Clamp(lines + 1, 6, 30);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadTemplates();
            StateHasChanged();
        }
    }

    private async Task LoadTemplates()
    {
        _isLoading = true;
        _hasError = false;
        _errorMessage = null;

        try
        {
            _templates = await Mediator.Send(new GetEmailTemplatesQuery());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load email templates");
            _hasError = true;
            _errorMessage = "We were unable to load the email templates. Please try again later.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SelectTemplate(string name)
    {
        _selectedName = name;
        _saveMessage = null;
        _saveError = null;
        _resetMessage = null;
        _resetError = null;
        _previewOutput = null;
        _previewError = null;
        _isPreviewing = false;
        // Selecting another template always returns to edit mode.
        _isPreviewMode = false;

        try
        {
            var template = await Mediator.Send(new GetEmailTemplateByNameQuery(name));
            if (template is null)
            {
                _notFound = true;
                _editorContent = string.Empty;
                return;
            }

            _notFound = false;
            _editorContent = template.Content;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load email template {TemplateName}", name);
            _saveError = "We were unable to load this template. Please try again later.";
        }

        StateHasChanged();
    }

    private async Task SaveTemplate()
    {
        if (_selectedName is null)
            return;

        _isSaving = true;
        _saveMessage = null;
        _saveError = null;
        // At most one save/reset notification is shown at any time, so a save
        // clears any previous reset notification.
        _resetMessage = null;
        _resetError = null;

        try
        {
            var result = await Mediator.Send(new UpdateEmailTemplateCommand(_selectedName, _editorContent));
            if (!result)
            {
                _saveError = "The template could not be saved. It may have been removed.";
            }
            else
            {
                _saveMessage = "Template saved successfully.";
                await LoadTemplates();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save email template {TemplateName}", _selectedName);
            _saveError = "We were unable to save the template. Please check the placeholders and try again.";
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private async Task PreviewTemplate()
    {
        if (_selectedName is null)
            return;

        _isPreviewing = true;
        _previewError = null;
        _previewOutput = null;

        try
        {
            _previewOutput = await Mediator.Send(new PreviewEmailTemplateQuery(_editorContent));
            // Only enter preview display mode after the preview generated successfully.
            _isPreviewMode = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to preview email template {TemplateName}", _selectedName);
            // Remain in edit mode; show the existing preview error.
            _isPreviewMode = false;
            _previewError = "We were unable to render the preview. Please try again later.";
        }
        finally
        {
            _isPreviewing = false;
            StateHasChanged();
        }
    }

    // Leaves preview mode and restores the editing workspace without reloading or
    // discarding the administrator's current (unsaved) edits.
    private void EditTemplate()
    {
        _isPreviewMode = false;
        _previewError = null;
    }

    private async Task ResetTemplate()
    {
        if (_selectedName is null)
            return;

        _isResetting = true;
        _resetMessage = null;
        _resetError = null;
        // At most one save/reset notification is shown at any time, so a reset
        // clears any previous save notification.
        _saveMessage = null;
        _saveError = null;

        try
        {
            var result = await Mediator.Send(new ResetEmailTemplateCommand(_selectedName));
            if (!result)
            {
                _resetError = "The template could not be reset. It may have been removed.";
            }
            else
            {
                _resetMessage = "Template reset to its default.";
                _resetError = null;

                // Reload the list and refresh the editor with the now-active default.
                await LoadTemplates();
                var template = await Mediator.Send(new GetEmailTemplateByNameQuery(_selectedName));
                _editorContent = template?.Content ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reset email template {TemplateName}", _selectedName);
            _resetError = "We were unable to reset the template. Please try again later.";
        }
        finally
        {
            _isResetting = false;
            StateHasChanged();
        }
    }
}
