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

    // Preview state.
    private bool _isPreviewing;
    private string? _previewOutput;
    private string? _previewError;

    private IReadOnlyList<string> SupportedPlaceholders => KnownEmailPlaceholders.All;

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
        _previewOutput = null;
        _previewError = null;
        _isPreviewing = false;

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
            Logger.LogError(ex, "Failed to load email template {Name}", name);
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
            Logger.LogError(ex, "Failed to save email template {Name}", _selectedName);
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
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to preview email template {Name}", _selectedName);
            _previewError = "We were unable to render the preview. Please try again later.";
        }
        finally
        {
            _isPreviewing = false;
            StateHasChanged();
        }
    }
}
